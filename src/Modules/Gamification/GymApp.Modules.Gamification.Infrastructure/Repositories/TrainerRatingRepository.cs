using GymApp.Modules.Gamification.Domain.Entities;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using GymApp.Modules.Gamification.Domain.ValueObjects;

namespace GymApp.Modules.Gamification.Infrastructure.Repositories;

internal sealed class TrainerRatingRepository : ITrainerRatingRepository
{
    private readonly GamificationDbContext _dbContext;

    public TrainerRatingRepository(GamificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TrainerRating?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrainerRatings
            .AsNoTracking()
            .FirstOrDefaultAsync(tr => tr.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TrainerRating>> GetByTrainerAsync(
        Guid gymId,
        Guid trainerId,
        DateTimeRange range,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrainerRatings
            .AsNoTracking()
            .Where(tr => tr.GymId == gymId
                && tr.TrainerId == trainerId
                && tr.CreatedAt >= range.From
                && tr.CreatedAt <= range.To)
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TrainerRating?> GetBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrainerRatings
            .AsNoTracking()
            .FirstOrDefaultAsync(tr => tr.SessionId == sessionId, cancellationToken);
    }

    public async Task<decimal> GetAverageAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var ratings = await _dbContext.TrainerRatings
            .AsNoTracking()
            .Where(tr => tr.TrainerId == trainerId && tr.GymId == gymId)
            .ToListAsync(cancellationToken);

        return ratings.Count > 0
            ? (decimal)ratings.Average(r => r.Stars)
            : 0m;
    }

    public async Task AddAsync(TrainerRating rating, CancellationToken cancellationToken = default)
    {
        await _dbContext.TrainerRatings.AddAsync(rating, cancellationToken);
    }
}
