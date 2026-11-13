using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Application.IntegrationEvents;
using GymApp.Modules.Billing.Application.Services;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Application.Commands.TrialFollowUps;

internal sealed class SendTrialFollowUpsCommandHandler : ICommandHandler<SendTrialFollowUpsCommand, int>
{
    private static readonly string[] RequiredFlags = { FeatureKeys.PtTrialSessions, FeatureKeys.PtTrialFollowUps };

    private readonly ISessionPackageRepository _packages;
    private readonly IPackageInterestRepository _interests;
    private readonly IFeatureFlagService _featureFlags;
    private readonly IIntegrationEventPublisher _integrationEvents;
    private readonly IDateTimeProvider _clock;
    private readonly IBillingUnitOfWork _unitOfWork;

    public SendTrialFollowUpsCommandHandler(
        ISessionPackageRepository packages,
        IPackageInterestRepository interests,
        IFeatureFlagService featureFlags,
        IIntegrationEventPublisher integrationEvents,
        IDateTimeProvider clock,
        IBillingUnitOfWork unitOfWork)
    {
        _packages = packages;
        _interests = interests;
        _featureFlags = featureFlags;
        _integrationEvents = integrationEvents;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(SendTrialFollowUpsCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var trials = await _packages.GetFinishedTrialsAsync(now - TrialFollowUpSchedule.LookBack, cancellationToken);

        var raised = 0;
        foreach (var gymTrials in trials.GroupBy(t => t.GymId))
        {
            if (!await IsEnabledAsync(gymTrials.Key, cancellationToken))
                continue;

            foreach (var trial in gymTrials)
                raised += await RaiseIfDueAsync(trial, now, cancellationToken) ? 1 : 0;
        }

        if (raised > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(raised);
    }

    private async Task<bool> IsEnabledAsync(Guid gymId, CancellationToken cancellationToken)
    {
        foreach (var key in RequiredFlags)
        {
            if (!await _featureFlags.IsEnabledAsync(gymId, key, cancellationToken))
                return false;
        }

        return true;
    }

    private async Task<bool> RaiseIfDueAsync(SessionPackage trial, DateTime now, CancellationToken cancellationToken)
    {
        if (TrialFollowUpSchedule.DueOn(trial.ExhaustedAt!.Value, now) is not { } due)
            return false;

        if (await HasMovedOnAsync(trial, cancellationToken) || !trial.TryMarkFollowUp(due))
            return false;

        PackageMilestonePublisher.Enqueue(_integrationEvents, trial);
        return true;
    }

    /// <summary>Bought a paid package or already tapped "I'm interested" since the trial started: no more reminders.</summary>
    private async Task<bool> HasMovedOnAsync(SessionPackage trial, CancellationToken cancellationToken)
    {
        var owner = new PackageOwner(trial.GymId, trial.MemberId);
        var paid = await HasPaidPackageSinceAsync(owner, trial.CreatedAt, cancellationToken);
        if (paid)
            return true;

        var interests = await _interests.GetByMemberAsync(owner, cancellationToken);
        return interests.Any(i => i.CreatedAt >= trial.CreatedAt);
    }

    private async Task<bool> HasPaidPackageSinceAsync(PackageOwner owner, DateTime sinceUtc, CancellationToken cancellationToken)
    {
        var packages = await _packages.GetByMemberAsync(owner.GymId, owner.MemberId, cancellationToken);
        return packages.Any(p => !p.IsTrial && p.CreatedAt >= sinceUtc);
    }
}
