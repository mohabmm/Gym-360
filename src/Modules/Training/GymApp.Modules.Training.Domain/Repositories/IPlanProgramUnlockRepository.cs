using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Domain.Repositories;

/// <summary>pt.starter_plan: the staff's per coach–member program unlocks.</summary>
public interface IPlanProgramUnlockRepository
{
    /// <summary>The pair's row (tracked), or null when the staff never unlocked it.</summary>
    Task<PlanProgramUnlock?> GetAsync(CoachTrainee pair, CancellationToken cancellationToken = default);

    /// <summary>The length the staff unlocked for this pair, or null when it is locked (read-only).</summary>
    Task<UnlockLength?> GetOpenLengthAsync(CoachTrainee pair, CancellationToken cancellationToken = default);

    /// <summary>Every coach's row for one member (read-only).</summary>
    Task<IReadOnlyList<PlanProgramUnlock>> ListForTraineeAsync(Guid gymId, Guid traineeId, CancellationToken cancellationToken = default);

    Task AddAsync(PlanProgramUnlock unlock, CancellationToken cancellationToken = default);
}
