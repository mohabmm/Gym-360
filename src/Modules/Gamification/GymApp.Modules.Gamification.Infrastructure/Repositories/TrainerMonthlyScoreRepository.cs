using GymApp.Modules.Gamification.Domain.Entities;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using GymApp.Modules.Gamification.Domain.ValueObjects;

namespace GymApp.Modules.Gamification.Infrastructure.Repositories;

internal sealed class TrainerMonthlyScoreRepository : ITrainerMonthlyScoreRepository
{
    private readonly GamificationDbContext _dbContext;

    public TrainerMonthlyScoreRepository(GamificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TrainerMonthlyScore?> GetByTrainerAsync(
        Guid gymId,
        Guid trainerId,
        LeaderboardPeriod period,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrainerMonthlyScores
            .FirstOrDefaultAsync(
                tms => tms.GymId == gymId
                    && tms.TrainerId == trainerId
                    && tms.Month == period.Month
                    && tms.Year == period.Year,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TrainerMonthlyScore>> GetByGymAsync(
        Guid gymId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrainerMonthlyScores
            .AsNoTracking()
            .Where(tms => tms.GymId == gymId && tms.Month == month && tms.Year == year)
            .OrderByDescending(tms => tms.NormalizedScore)
            .ToListAsync(cancellationToken);
    }

    public async Task<TrainerMonthlyScore?> GetCoachOfMonthAsync(
        Guid gymId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrainerMonthlyScores
            .AsNoTracking()
            .Where(tms => tms.GymId == gymId
                && tms.Month == month
                && tms.Year == year
                && tms.IsCoachOfTheMonth)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(TrainerMonthlyScore score, CancellationToken cancellationToken = default)
    {
        await _dbContext.TrainerMonthlyScores.AddAsync(score, cancellationToken);
    }

    public void Update(TrainerMonthlyScore score)
    {
        _dbContext.TrainerMonthlyScores.Update(score);
    }
}
