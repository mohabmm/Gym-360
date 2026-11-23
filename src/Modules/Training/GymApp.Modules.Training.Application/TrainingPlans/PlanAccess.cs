using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.TrainingPlans;

/// <summary>Who is acting: the user, the gym from the token, and whether they are its owner or a manager.</summary>
public sealed record PlanActor(Guid GymId, Guid UserId, bool IsGymManager = false)
{
    public TemplateUser AsTemplateUser => new(GymId, UserId, IsGymManager);
}

/// <summary>Feature gate and "is this trainee my client" checks shared by every plan handler.</summary>
internal sealed class PlanAccess
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly ITrainerAssignmentLookup _assignments;

    public PlanAccess(IFeatureFlagService featureFlags, ITrainerAssignmentLookup assignments)
    {
        _featureFlags = featureFlags;
        _assignments = assignments;
    }

    /// <summary><c>FEATURE_DISABLED:pt.training_plan</c> when the gym has plans switched off.</summary>
    public async Task<Result> EnsureEnabledAsync(Guid gymId, CancellationToken cancellationToken) =>
        await _featureFlags.IsEnabledAsync(gymId, FeatureKeys.PtTrainingPlan, cancellationToken)
            ? Result.Success()
            : Result.Failure(FeatureFlagErrors.FeatureDisabled(FeatureKeys.PtTrainingPlan));

    /// <summary>
    /// A coach works with a trainee who has an active package with them — free sessions included.
    /// The coach who assigned a plan keeps access to it.
    /// </summary>
    public async Task<bool> IsCoachOfAsync(PlanActor actor, Guid traineeId, CancellationToken cancellationToken)
    {
        var clients = await _assignments.GetClientsForTrainerAsync(actor.UserId, actor.GymId, cancellationToken);
        return clients.Contains(traineeId);
    }

    public async Task<bool> CanManageAsync(PlanActor actor, TrainingPlan plan, CancellationToken cancellationToken) =>
        plan.TrainerId == actor.UserId || await IsCoachOfAsync(actor, plan.TraineeId, cancellationToken);

    /// <summary>The trainee, their coach, or the gym's owner/manager can read a plan.</summary>
    public async Task<bool> CanReadAsync(PlanActor actor, TrainingPlan plan, CancellationToken cancellationToken) =>
        plan.TraineeId == actor.UserId || actor.IsGymManager || await CanManageAsync(actor, plan, cancellationToken);

    public static Result<T> Forbidden<T>() => Result.Failure<T>(TrainingPlanErrors.PlanForbidden);

    public static bool CanEdit(PlanTemplate template, PlanActor actor) =>
        template.CanBeEditedBy(actor.AsTemplateUser);
}
