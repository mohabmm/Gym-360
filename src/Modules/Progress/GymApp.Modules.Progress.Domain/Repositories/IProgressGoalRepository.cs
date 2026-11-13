using GymApp.Modules.Progress.Domain.Entities;

namespace GymApp.Modules.Progress.Domain.Repositories;

public interface IProgressGoalRepository
{
    /// <summary>Tracked, for updates.</summary>
    Task<ProgressGoal?> GetAsync(Guid userId, Guid gymId, CancellationToken cancellationToken = default);

    /// <summary>Read-only (no tracking).</summary>
    Task<ProgressGoal?> FindAsync(Guid userId, Guid gymId, CancellationToken cancellationToken = default);

    Task AddAsync(ProgressGoal goal, CancellationToken cancellationToken = default);
}
