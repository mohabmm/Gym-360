using GymApp.Modules.Gamification.Domain.Entities;
using GymApp.Modules.Gamification.Domain.ValueObjects;

namespace GymApp.Modules.Gamification.Domain.Repositories;

public interface ITrainerMonthlyScoreRepository
{
    Task<TrainerMonthlyScore?> GetByTrainerAsync(
        Guid gymId,
        Guid trainerId,
        LeaderboardPeriod period,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainerMonthlyScore>> GetByGymAsync(
        Guid gymId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    Task<TrainerMonthlyScore?> GetCoachOfMonthAsync(
        Guid gymId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TrainerMonthlyScore score,
        CancellationToken cancellationToken = default);

    void Update(TrainerMonthlyScore score);
}
