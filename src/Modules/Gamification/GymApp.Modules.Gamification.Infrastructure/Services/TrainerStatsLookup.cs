using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gamification.Infrastructure.Services;

internal sealed class TrainerStatsLookup : ITrainerStatsLookup
{
    private readonly GamificationDbContext _dbContext;

    public TrainerStatsLookup(GamificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TrainerStats> GetAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var rating = await _dbContext.TrainerPublicProfiles
            .AsNoTracking()
            .Where(tpp => tpp.TrainerId == trainerId && tpp.GymId == gymId)
            .Select(tpp => (decimal?)tpp.DisplayRating)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;

        var now = DateTime.UtcNow;
        var monthlyScore = await _dbContext.TrainerMonthlyScores
            .AsNoTracking()
            .Where(tms => tms.TrainerId == trainerId
                          && tms.GymId == gymId
                          && tms.Month == now.Month
                          && tms.Year == now.Year)
            .Select(tms => (decimal?)tms.NormalizedScore)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;

        return new TrainerStats(rating, monthlyScore);
    }

    public async Task<IReadOnlyDictionary<Guid, TrainerStats>> GetBatchAsync(
        IReadOnlyCollection<Guid> trainerIds,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        if (trainerIds.Count == 0)
            return new Dictionary<Guid, TrainerStats>();

        var ids = trainerIds.Distinct().ToList();

        var ratings = await _dbContext.TrainerPublicProfiles
            .AsNoTracking()
            .Where(tpp => tpp.GymId == gymId && ids.Contains(tpp.TrainerId))
            .Select(tpp => new { tpp.TrainerId, tpp.DisplayRating })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var scores = await _dbContext.TrainerMonthlyScores
            .AsNoTracking()
            .Where(tms => tms.GymId == gymId
                          && ids.Contains(tms.TrainerId)
                          && tms.Month == now.Month
                          && tms.Year == now.Year)
            .Select(tms => new { tms.TrainerId, tms.NormalizedScore })
            .ToListAsync(cancellationToken);

        var ratingMap = ratings.ToDictionary(r => r.TrainerId, r => r.DisplayRating);
        var scoreMap = scores.ToDictionary(s => s.TrainerId, s => s.NormalizedScore);

        return ids.ToDictionary(
            id => id,
            id => new TrainerStats(
                ratingMap.TryGetValue(id, out var rating) ? rating : 0m,
                scoreMap.TryGetValue(id, out var score) ? score : 0m));
    }
}
