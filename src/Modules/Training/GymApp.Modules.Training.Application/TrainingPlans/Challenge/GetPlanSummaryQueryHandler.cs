using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.TrainingPlans.Workouts;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Challenge;

/// <summary>pt.owner_dashboard: active plans, this month's plan workouts, and who reached the challenge.</summary>
internal sealed class GetPlanSummaryQueryHandler : IQueryHandler<GetPlanSummaryQuery, PlanSummaryDto>
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly ChallengeAccess _access;
    private readonly ITrainingPlanRepository _plans;
    private readonly IPlanCompletionRepository _completions;
    private readonly IChallengeAwardRepository _awards;

    public GetPlanSummaryQueryHandler(
        IFeatureFlagService featureFlags,
        ChallengeAccess access,
        ITrainingPlanRepository plans,
        IPlanCompletionRepository completions,
        IChallengeAwardRepository awards)
    {
        _featureFlags = featureFlags;
        _access = access;
        _plans = plans;
        _completions = completions;
        _awards = awards;
    }

    public async Task<Result<PlanSummaryDto>> Handle(GetPlanSummaryQuery request, CancellationToken cancellationToken)
    {
        var gymId = request.Actor.GymId;
        foreach (var key in new[] { FeatureKeys.PtOwnerDashboard, FeatureKeys.PtTrainingPlan })
        {
            if (!await _featureFlags.IsEnabledAsync(gymId, key, cancellationToken))
                return Result.Failure<PlanSummaryDto>(FeatureFlagErrors.FeatureDisabled(key));
        }

        var today = TrainingCalendar.Today();
        var month = await _completions.CountSinceAsync(gymId, new DateOnly(today.Year, today.Month, 1), cancellationToken);
        var summary = new PlanSummaryDto
        {
            ActivePlans = await _plans.CountActiveAsync(gymId, cancellationToken),
            WorkoutsThisMonth = month.Workouts,
            MembersTrainingThisMonth = month.Members,
            ChallengeEnabled = await _access.IsChallengeOnAsync(gymId, cancellationToken)
        };
        if (!summary.ChallengeEnabled)
            return Result.Success(summary);

        var awards = await _awards.ListAsync(gymId, null, cancellationToken);
        return Result.Success(summary with
        {
            ChallengeReached = awards.Count,
            ChallengeAwaitingDelivery = awards.Count(a => !a.IsDelivered)
        });
    }
}
