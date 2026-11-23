using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Application.TrainingPlans.Challenge;

/// <summary>The challenge needs plans (pt.training_plan) and its own pilot flag (mem.first_month_challenge).</summary>
internal sealed class ChallengeAccess
{
    private readonly PlanAccess _plans;
    private readonly IFeatureFlagService _featureFlags;

    public ChallengeAccess(PlanAccess plans, IFeatureFlagService featureFlags)
    {
        _plans = plans;
        _featureFlags = featureFlags;
    }

    public async Task<Result> EnsureEnabledAsync(Guid gymId, CancellationToken cancellationToken)
    {
        var plans = await _plans.EnsureEnabledAsync(gymId, cancellationToken);
        if (plans.IsFailure)
            return plans;
        return await IsChallengeOnAsync(gymId, cancellationToken)
            ? Result.Success()
            : Result.Failure(FeatureFlagErrors.FeatureDisabled(FeatureKeys.MemFirstMonthChallenge));
    }

    public Task<bool> IsChallengeOnAsync(Guid gymId, CancellationToken cancellationToken) =>
        _featureFlags.IsEnabledAsync(gymId, FeatureKeys.MemFirstMonthChallenge, cancellationToken);
}
