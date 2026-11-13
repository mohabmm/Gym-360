using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Modules.Billing.Application.Commands.TrialFollowUps;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace GymApp.Modules.Billing.Infrastructure.Jobs;

/// <summary>
/// pt.trial_followups: daily, raises the day-3 / day-12 reminders for trainees who finished a
/// free trial without subscribing. Tenancy-aware like <see cref="ProcessExpiredSessionPackagesJob"/>:
/// in Hard mode each gym runs in its own tenant scope. One gym failing never stops the others.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class SendTrialFollowUpsJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TenancyOptions _tenancyOptions;
    private readonly ILogger<SendTrialFollowUpsJob> _logger;

    public SendTrialFollowUpsJob(
        IServiceScopeFactory scopeFactory,
        IOptions<TenancyOptions> tenancyOptions,
        ILogger<SendTrialFollowUpsJob> logger)
    {
        _scopeFactory = scopeFactory;
        _tenancyOptions = tenancyOptions.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        if (_tenancyOptions.Mode != TenancyMode.Hard)
        {
            await RunAsync(null, ct);
            return;
        }

        IReadOnlyList<GymRegistration> gyms;
        using (var registryScope = _scopeFactory.CreateScope())
            gyms = await registryScope.ServiceProvider.GetRequiredService<IGymRegistry>().GetActiveAsync(ct);

        foreach (var gym in gyms)
            await RunAsync(gym, ct);
    }

    private async Task RunAsync(GymRegistration? gym, CancellationToken ct)
    {
        var prefix = gym?.SchemaPrefix ?? "soft";
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            if (gym is not null)
                sp.GetRequiredService<ITenantContextAccessor>().Set(gym.GymId, gym.SchemaPrefix, isOwnerScope: false);

            var result = await sp.GetRequiredService<ISender>().Send(new SendTrialFollowUpsCommand(), ct);
            _logger.LogInformation("SendTrialFollowUpsJob[{Prefix}]: {Count} reminder(s) raised.", prefix, result.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "SendTrialFollowUpsJob[{Prefix}]: failed.", prefix);
        }
    }
}
