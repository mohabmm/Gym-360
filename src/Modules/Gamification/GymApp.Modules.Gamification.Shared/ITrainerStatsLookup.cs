namespace GymApp.Modules.Gamification.Shared;

public sealed record TrainerStats(decimal Rating, decimal MonthlyScore);

/// <summary>
/// Aggregated trainer scoring (display rating + current-month normalized score)
/// owned by the Gamification module. Exposed via the Gamification .Api project
/// so other modules (Gyms) can render KPI cards without referencing
/// Gamification.Application.
/// </summary>
public interface ITrainerStatsLookup
{
    Task<TrainerStats> GetAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, TrainerStats>> GetBatchAsync(
        IReadOnlyCollection<Guid> trainerIds,
        Guid gymId,
        CancellationToken cancellationToken = default);
}
