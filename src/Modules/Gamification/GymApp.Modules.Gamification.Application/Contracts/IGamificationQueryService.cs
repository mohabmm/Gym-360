using GymApp.Modules.Gamification.Domain.ValueObjects;
namespace GymApp.Modules.Gamification.Application.Contracts;

public interface IGamificationQueryService
{
    Task<decimal> GetTrainerDisplayRatingAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default);

    Task<int?> GetMemberLeaderboardPositionAsync(
        Guid memberId,
        Guid gymId,
        string leaderboardType,
        CancellationToken cancellationToken = default);

    Task<bool> IsCoachOfTheMonthAsync(
        Guid trainerId,
        Guid gymId,
        LeaderboardPeriod period,
        CancellationToken cancellationToken = default);
}
