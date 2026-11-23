using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Domain.Repositories;

public interface IPlanCompletionRepository
{
    /// <summary>The workout this device already uploaded under <paramref name="clientId"/>, if any.</summary>
    Task<PlanWorkoutCompletion?> GetByClientIdAsync(Guid traineeId, Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>Every workout logged against the plan, voided ones included (read-only).</summary>
    Task<IReadOnlyList<PlanWorkoutCompletion>> ListForPlanAsync(Guid gymId, Guid planId, CancellationToken cancellationToken = default);

    /// <summary>Workouts recorded from this PT session (tracked, so they can be voided).</summary>
    Task<IReadOnlyList<PlanWorkoutCompletion>> ListForSessionAsync(Guid gymId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>The trainee's workouts at this gym on or after <paramref name="since"/>, newest first (read-only).</summary>
    Task<IReadOnlyList<PlanWorkoutCompletion>> ListForTraineeAsync(Guid gymId, Guid traineeId, DateOnly? since, CancellationToken cancellationToken = default);

    /// <summary>Workouts of these trainees at this gym on or after <paramref name="since"/> (read-only).</summary>
    Task<IReadOnlyList<PlanWorkoutCompletion>> ListForTraineesAsync(Guid gymId, IReadOnlyCollection<Guid> traineeIds, DateOnly since, CancellationToken cancellationToken = default);

    /// <summary>Every workout of the trainee in any gym of this scope (data export).</summary>
    Task<IReadOnlyList<PlanWorkoutCompletion>> ListAllForTraineeAsync(Guid traineeId, CancellationToken cancellationToken = default);

    /// <summary>Workouts that count (not voided) at this gym on or after <paramref name="since"/>, and how many members did them.</summary>
    Task<PlanWorkoutTotals> CountSinceAsync(Guid gymId, DateOnly since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the new workout. False when another upload with the same client id won the race
    /// (the unique index rejected it); nothing is saved then.
    /// </summary>
    Task<bool> TryAddAsync(PlanWorkoutCompletion completion, CancellationToken cancellationToken = default);
}

public sealed record PlanWorkoutTotals(int Workouts, int Members);
