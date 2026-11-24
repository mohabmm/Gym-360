using GymApp.Common.Application.Errors;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Nutrition.Domain.Diet;

namespace GymApp.Modules.Nutrition.Application.Diets;

/// <summary>
/// Gates for the ready diets: the coach side needs <c>meal_plans</c>, the member side
/// <c>nutrition_tracking</c> (checked before any work, ironclad rule 2). A coach only works with
/// members who have an active package with them — the same rule as training plans.
/// </summary>
internal sealed class DietAccess
{
    private const string MealPlans = "meal_plans";
    private const string Tracking = "nutrition_tracking";

    private readonly IGymPlanService _plans;
    private readonly ITrainerAssignmentLookup _assignments;

    public DietAccess(IGymPlanService plans, ITrainerAssignmentLookup assignments)
    {
        _plans = plans;
        _assignments = assignments;
    }

    public Task<Result> EnsureMealPlansAsync(Guid gymId, CancellationToken cancellationToken) =>
        EnsureAsync(gymId, MealPlans, cancellationToken);

    public Task<Result> EnsureTrackingAsync(Guid gymId, CancellationToken cancellationToken) =>
        EnsureAsync(gymId, Tracking, cancellationToken);

    /// <summary>The feature is on and the member is the coach's client.</summary>
    public async Task<Result> EnsureCoachOfAsync(DietCoach coach, Guid clientId, CancellationToken cancellationToken)
    {
        var enabled = await EnsureMealPlansAsync(coach.GymId, cancellationToken);
        if (enabled.IsFailure)
            return enabled;
        var clients = await _assignments.GetClientsForTrainerAsync(coach.TrainerId, coach.GymId, cancellationToken);
        return clients.Contains(clientId) ? Result.Success() : Result.Failure(DietErrors.NotYourClient);
    }

    private async Task<Result> EnsureAsync(Guid gymId, string feature, CancellationToken cancellationToken)
    {
        if (await _plans.IsFeatureEnabledAsync(gymId, feature, cancellationToken))
            return Result.Success();
        var plan = await _plans.GetPlanCodeAsync(gymId, cancellationToken);
        return Result.Failure(PlanErrors.FeatureNotAvailable(feature, plan, "Pro"));
    }
}
