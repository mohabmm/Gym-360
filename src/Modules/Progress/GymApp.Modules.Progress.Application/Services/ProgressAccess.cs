using GymApp.Common.Application.Errors;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Progress.Domain;

namespace GymApp.Modules.Progress.Application.Services;

/// <summary>Shared gate for progress handlers: feature on for the gym, and coach-to-client access.</summary>
internal static class ProgressAccess
{
    private const string FeatureCode = "progress_tracking";

    /// <summary>Error.None when progress tracking is on for the gym.</summary>
    public static async Task<Error> CheckFeatureAsync(
        IGymPlanService planService, Guid gymId, CancellationToken cancellationToken)
    {
        if (await planService.IsFeatureEnabledAsync(gymId, FeatureCode, cancellationToken))
            return Error.None;

        var currentPlan = await planService.GetPlanCodeAsync(gymId, cancellationToken);
        return PlanErrors.FeatureNotAvailable(FeatureCode, currentPlan, "Pro");
    }

    /// <summary>
    /// Error.None when the caller may see the member's progress: the member themself
    /// (no trainer) or a coach the member has an active package with.
    /// </summary>
    public static async Task<Error> CheckCoachAsync(
        ITrainerAssignmentLookup assignments, ProgressSubject subject, CancellationToken cancellationToken)
    {
        if (subject.TrainerId is not { } trainerId)
            return Error.None;

        var clients = await assignments.GetClientsForTrainerAsync(trainerId, subject.GymId, cancellationToken);
        return clients.Contains(subject.MemberId) ? Error.None : ProgressErrors.NotClientOfTrainer;
    }

    /// <summary>Runs both checks in order; Error.None when allowed.</summary>
    public static async Task<Error> CheckAsync(
        IGymPlanService planService,
        ITrainerAssignmentLookup assignments,
        ProgressSubject subject,
        CancellationToken cancellationToken)
    {
        var feature = await CheckFeatureAsync(planService, subject.GymId, cancellationToken);
        if (feature != Error.None)
            return feature;

        return await CheckCoachAsync(assignments, subject, cancellationToken);
    }
}

/// <summary>Whose progress is read or changed, and by which coach (null = the member).</summary>
internal sealed record ProgressSubject(Guid MemberId, Guid GymId, Guid? TrainerId);
