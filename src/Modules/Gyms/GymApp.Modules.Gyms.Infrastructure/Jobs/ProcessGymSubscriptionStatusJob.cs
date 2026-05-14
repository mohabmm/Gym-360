using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy;
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
        var now = DateTime.UtcNow;

        // Load the billing read models once so we can correlate by GymId
        var subscriptions = await dbContext.GymSubscriptions
            .AsNoTracking()
            .ToListAsync(ct);

        var subscriptionByGymId = subscriptions
            .GroupBy(s => s.GymId)
            .ToDictionary(g => g.Key, g => g.First());

        // Step 1: Active → Overdue (NextPaymentDue + 3 days ago)
        var overdueThreshold = now.AddDays(-3);
        var activeGymIds = subscriptions
            .Where(s => s.PaymentStatus == "Paid"
                        && s.NextPaymentDue.HasValue
                        && s.NextPaymentDue.Value < overdueThreshold)
            .Select(s => s.GymId)
            .ToHashSet();

        if (activeGymIds.Count > 0)
        {
            var activeGyms = await dbContext.Gyms
                .Where(g => g.Status == GymStatus.Active && activeGymIds.Contains(g.Id))
                .ToListAsync(ct);

            foreach (var g in activeGyms)
            {
                var result = g.MarkOverdue();
                if (result.IsFailure)
                    _logger.LogWarning("ProcessGymSubscriptionStatusJob: MarkOverdue failed for gym {GymId}: {Error}", g.Id, result.Error);
                else
                    _logger.LogInformation("ProcessGymSubscriptionStatusJob: Gym {GymId} marked Overdue.", g.Id);
            }
        }

        // Step 2: Overdue → GracePeriod (overdue for more than 7 days)
        var gracePeriodThreshold = now.AddDays(-7);
        var overdueGymIds = subscriptions
            .Where(s => s.PaymentStatus == "Overdue"
                        && s.OverdueSince.HasValue
                        && s.OverdueSince.Value < gracePeriodThreshold)
            .Select(s => s.GymId)
            .ToHashSet();

        if (overdueGymIds.Count > 0)
        {
            var overdueGyms = await dbContext.Gyms
                .Where(g => g.Status == GymStatus.Active
                            && g.PaymentStatus == PaymentStatus.Overdue
                            && overdueGymIds.Contains(g.Id))
                .ToListAsync(ct);

            foreach (var g in overdueGyms)
            {
                var gracePeriodEndsAt = now.AddDays(14);
                var result = g.EnterGracePeriod(gracePeriodEndsAt);
                if (result.IsFailure)
                    _logger.LogWarning("ProcessGymSubscriptionStatusJob: EnterGracePeriod failed for gym {GymId}: {Error}", g.Id, result.Error);
                else
                    _logger.LogInformation("ProcessGymSubscriptionStatusJob: Gym {GymId} entered GracePeriod (ends {EndsAt}).", g.Id, gracePeriodEndsAt);
            }
        }

        // Step 3: GracePeriod → Suspended (GracePeriodEndsAt has passed)
        var gracePeriodGymIds = subscriptions
            .Where(s => s.PaymentStatus == "GracePeriod"
                        && s.GracePeriodEndsAt.HasValue
                        && s.GracePeriodEndsAt.Value < now)
            .Select(s => s.GymId)
            .ToHashSet();

        if (gracePeriodGymIds.Count > 0)
        {
            var gracePeriodGyms = await dbContext.Gyms
                .Where(g => g.Status == GymStatus.GracePeriod && gracePeriodGymIds.Contains(g.Id))
                .ToListAsync(ct);

            foreach (var g in gracePeriodGyms)
            {
                var result = g.Suspend("Auto-suspended: payment overdue beyond grace period");
                if (result.IsFailure)
                    _logger.LogWarning("ProcessGymSubscriptionStatusJob: Suspend failed for gym {GymId}: {Error}", g.Id, result.Error);
                else
                    _logger.LogInformation("ProcessGymSubscriptionStatusJob: Gym {GymId} suspended (grace period expired).", g.Id);
            }
        }

        // Step 4: Suspended → Terminated (suspended for more than 30 days)
        var terminationThreshold = now.AddDays(-30);
        var suspendedGyms = await dbContext.Gyms
            .Where(g => g.Status == GymStatus.Suspended
                        && g.SuspendedAt.HasValue
                        && g.SuspendedAt.Value < terminationThreshold)
            .ToListAsync(ct);

        foreach (var g in suspendedGyms)
        {
            var result = g.Terminate();
            if (result.IsFailure)
                _logger.LogWarning("ProcessGymSubscriptionStatusJob: Terminate failed for gym {GymId}: {Error}", g.Id, result.Error);
            else
                _logger.LogInformation("ProcessGymSubscriptionStatusJob: Gym {GymId} terminated (suspended 30+ days).", g.Id);
        }

        await dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ProcessGymSubscriptionStatusJob[{Prefix}]: Completed. Overdue: {Overdue}, Grace: {Grace}, Suspended: {Suspend}, Terminated: {Terminated}.",
            gym?.SchemaPrefix ?? "soft",
            activeGymIds.Count, overdueGymIds.Count, gracePeriodGymIds.Count, suspendedGyms.Count);
    }
}
