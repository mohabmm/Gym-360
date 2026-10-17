using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace GymApp.Modules.Gyms.Infrastructure.Jobs;

/// <summary>
/// Background job that auto-transitions gym billing states based on payment due dates.
/// Runs every 6 hours and progresses gyms through:
///   Active → Overdue (payment 3+ days late)
///   Overdue → GracePeriod (overdue 7+ days)
///   GracePeriod → Suspended (grace period 14+ days past start)
///   Suspended → Terminated (suspended 30+ days)
///
/// Tenancy-aware: in Hard mode, iterates gyms_registry and processes each gym's
/// subscription state inside its own tenant scope.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class ProcessGymSubscriptionStatusJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TenancyOptions _tenancyOptions;
    private readonly ILogger<ProcessGymSubscriptionStatusJob> _logger;

    public ProcessGymSubscriptionStatusJob(
        IServiceScopeFactory scopeFactory,
        IOptions<TenancyOptions> tenancyOptions,
        ILogger<ProcessGymSubscriptionStatusJob> logger)
    {
        _scopeFactory = scopeFactory;
        _tenancyOptions = tenancyOptions.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        if (_tenancyOptions.Mode == TenancyMode.Hard)
        {
            using var registryScope = _scopeFactory.CreateScope();
            var registry = registryScope.ServiceProvider.GetRequiredService<IGymRegistry>();
            var gyms = await registry.GetActiveAsync(ct);

            foreach (var gym in gyms)
                await ProcessAsync(gym, ct);
        }
        else
        {
            await ProcessAsync(null, ct);
        }
    }

    private async Task ProcessAsync(GymRegistration? gym, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        if (gym is not null)
        {
            var accessor = sp.GetRequiredService<ITenantContextAccessor>();
            accessor.Set(gym.GymId, gym.SchemaPrefix, isOwnerScope: false);
        }

        var dbContext = sp.GetRequiredService<GymsDbContext>();

        // Load the billing read models once so we can correlate by GymId
        var subscriptions = await dbContext.GymSubscriptions
            .AsNoTracking()
            .ToListAsync(ct);

        var sweep = new SweepContext(dbContext, subscriptions, DateTime.UtcNow);

        var overdue = await MarkOverdueAsync(sweep, ct);
        var grace = await EnterGracePeriodAsync(sweep, ct);
        var suspended = await SuspendExpiredGracePeriodsAsync(sweep, ct);
        var terminated = await TerminateLongSuspendedAsync(sweep, ct);

        await dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ProcessGymSubscriptionStatusJob[{Prefix}]: Completed. Overdue: {Overdue}, Grace: {Grace}, Suspended: {Suspend}, Terminated: {Terminated}.",
            gym?.SchemaPrefix ?? "soft",
            overdue, grace, suspended, terminated);
    }

    // Step 1: Active → Overdue (NextPaymentDue + 3 days ago)
    private async Task<int> MarkOverdueAsync(SweepContext sweep, CancellationToken ct)
    {
        var overdueThreshold = sweep.Now.AddDays(-3);
        var gymIds = sweep.GymIdsWhere(s => s.PaymentStatus == "Paid"
                                            && s.NextPaymentDue.HasValue
                                            && s.NextPaymentDue.Value < overdueThreshold);
        if (gymIds.Count == 0)
            return 0;

        var activeGyms = await sweep.Db.Gyms
            .Where(g => g.Status == GymStatus.Active && gymIds.Contains(g.Id))
            .ToListAsync(ct);

        foreach (var g in activeGyms)
            LogTransition(g.MarkOverdue(), nameof(Gym.MarkOverdue), g.Id,
                () => _logger.LogInformation("ProcessGymSubscriptionStatusJob: Gym {GymId} marked Overdue.", g.Id));

        return gymIds.Count;
    }

    // Step 2: Overdue → GracePeriod (overdue for more than 7 days)
    private async Task<int> EnterGracePeriodAsync(SweepContext sweep, CancellationToken ct)
    {
        var gracePeriodThreshold = sweep.Now.AddDays(-7);
        var gymIds = sweep.GymIdsWhere(s => s.PaymentStatus == "Overdue"
                                            && s.OverdueSince.HasValue
                                            && s.OverdueSince.Value < gracePeriodThreshold);
        if (gymIds.Count == 0)
            return 0;

        var overdueGyms = await sweep.Db.Gyms
            .Where(g => g.Status == GymStatus.Active
                        && g.PaymentStatus == PaymentStatus.Overdue
                        && gymIds.Contains(g.Id))
            .ToListAsync(ct);

        foreach (var g in overdueGyms)
        {
            var gracePeriodEndsAt = sweep.Now.AddDays(14);
            LogTransition(g.EnterGracePeriod(gracePeriodEndsAt), nameof(Gym.EnterGracePeriod), g.Id,
                () => _logger.LogInformation("ProcessGymSubscriptionStatusJob: Gym {GymId} entered GracePeriod (ends {EndsAt}).", g.Id, gracePeriodEndsAt));
        }

        return gymIds.Count;
    }

    // Step 3: GracePeriod → Suspended (GracePeriodEndsAt has passed)
    private async Task<int> SuspendExpiredGracePeriodsAsync(SweepContext sweep, CancellationToken ct)
    {
        var gymIds = sweep.GymIdsWhere(s => s.PaymentStatus == "GracePeriod"
                                            && s.GracePeriodEndsAt.HasValue
                                            && s.GracePeriodEndsAt.Value < sweep.Now);
        if (gymIds.Count == 0)
            return 0;

        var gracePeriodGyms = await sweep.Db.Gyms
            .Where(g => g.Status == GymStatus.GracePeriod && gymIds.Contains(g.Id))
            .ToListAsync(ct);

        foreach (var g in gracePeriodGyms)
            LogTransition(g.Suspend("Auto-suspended: payment overdue beyond grace period"), nameof(Gym.Suspend), g.Id,
                () => _logger.LogInformation("ProcessGymSubscriptionStatusJob: Gym {GymId} suspended (grace period expired).", g.Id));

        return gymIds.Count;
    }

    // Step 4: Suspended → Terminated (suspended for more than 30 days)
    private async Task<int> TerminateLongSuspendedAsync(SweepContext sweep, CancellationToken ct)
    {
        var terminationThreshold = sweep.Now.AddDays(-30);
        var suspendedGyms = await sweep.Db.Gyms
            .Where(g => g.Status == GymStatus.Suspended
                        && g.SuspendedAt.HasValue
                        && g.SuspendedAt.Value < terminationThreshold)
            .ToListAsync(ct);

        foreach (var g in suspendedGyms)
            LogTransition(g.Terminate(), nameof(Gym.Terminate), g.Id,
                () => _logger.LogInformation("ProcessGymSubscriptionStatusJob: Gym {GymId} terminated (suspended 30+ days).", g.Id));

        return suspendedGyms.Count;
    }

    private void LogTransition(Result result, string transition, Guid gymId, Action logSuccess)
    {
        if (result.IsFailure)
            _logger.LogWarning("ProcessGymSubscriptionStatusJob: {Transition} failed for gym {GymId}: {Error}", transition, gymId, result.Error);
        else
            logSuccess();
    }

    private sealed record SweepContext(GymsDbContext Db, List<GymSubscriptionReadModel> Subscriptions, DateTime Now)
    {
        public HashSet<Guid> GymIdsWhere(Func<GymSubscriptionReadModel, bool> predicate) =>
            Subscriptions.Where(predicate).Select(s => s.GymId).ToHashSet();
    }
}
