using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace GymApp.Modules.Billing.Infrastructure.Jobs;

/// <summary>
/// Background job that expires session packages whose ExpiresAt date has passed
/// and whose status is still Active. Runs daily.
/// Without this job, members could continue booking sessions on technically-expired packages.
///
/// Tenancy-aware: in Hard mode, iterates gyms_registry and processes each gym's
/// expired packages inside its own tenant scope.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class ProcessExpiredSessionPackagesJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TenancyOptions _tenancyOptions;
    private readonly ILogger<ProcessExpiredSessionPackagesJob> _logger;

    public ProcessExpiredSessionPackagesJob(
        IServiceScopeFactory scopeFactory,
        IOptions<TenancyOptions> tenancyOptions,
        ILogger<ProcessExpiredSessionPackagesJob> logger)
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

        var dbContext = sp.GetRequiredService<BillingDbContext>();
        var now = DateTime.UtcNow;

        var expiredPackages = await dbContext.SessionPackages
            .Where(p => p.Status == PackageStatus.Active
                        && p.ExpiresAt != null
                        && p.ExpiresAt < now)
            .ToListAsync(ct);

        foreach (var package in expiredPackages)
        {
            package.Expire();
            _logger.LogInformation(
                "ProcessExpiredSessionPackagesJob[{Prefix}]: Session package {PackageId} for member {MemberId} expired.",
                gym?.SchemaPrefix ?? "soft", package.Id, package.MemberId);
        }

        if (expiredPackages.Count > 0)
            await dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ProcessExpiredSessionPackagesJob[{Prefix}]: Completed. Packages expired: {Count}.",
            gym?.SchemaPrefix ?? "soft", expiredPackages.Count);
    }
}
