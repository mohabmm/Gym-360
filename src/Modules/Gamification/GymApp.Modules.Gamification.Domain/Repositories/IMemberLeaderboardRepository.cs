using GymApp.Modules.Gamification.Domain.Entities;
using GymApp.Modules.Gamification.Domain.Enums;
using GymApp.Modules.Gamification.Domain.ValueObjects;

namespace GymApp.Modules.Gamification.Domain.Repositories;

public interface IMemberLeaderboardRepository
{
    Task<MemberLeaderboard?> GetByGymAndTypeAsync(
        Guid gymId,
        LeaderboardType type,
        LeaderboardPeriod period,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MemberLeaderboard leaderboard,
        CancellationToken cancellationToken = default);

    void Update(MemberLeaderboard leaderboard);
}
