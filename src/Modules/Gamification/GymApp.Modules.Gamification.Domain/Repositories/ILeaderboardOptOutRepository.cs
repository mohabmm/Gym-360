using GymApp.Modules.Gamification.Domain.Entities;

namespace GymApp.Modules.Gamification.Domain.Repositories;

public interface ILeaderboardOptOutRepository
{
    Task<LeaderboardOptOut?> GetByUserAndGymAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        LeaderboardOptOut optOut,
        CancellationToken cancellationToken = default);

    void Update(LeaderboardOptOut optOut);
}
