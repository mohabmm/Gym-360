using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;

namespace GymApp.Modules.Progress.Domain.Repositories;

public interface IStreakRepository
{
    Task<Streak?> GetByUserAndTypeAsync(
        Guid userId,
        Guid gymId,
        StreakType streakType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Streak>> GetAllByUserAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Streak streak, CancellationToken cancellationToken = default);

    void Update(Streak streak);
}
