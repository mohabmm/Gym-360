using GymApp.Modules.Gamification.Application.Contracts;
using GymApp.Modules.Gamification.Domain.Enums;
using GymApp.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using GymApp.Modules.Gamification.Domain.ValueObjects;

namespace GymApp.Modules.Gamification.Infrastructure.Services;

internal sealed class GamificationQueryService : IGamificationQueryService
{
    private readonly GamificationDbContext _dbContext;

    public GamificationQueryService(GamificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetTrainerDisplayRatingAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.TrainerPublicProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                tpp => tpp.TrainerId == trainerId && tpp.GymId == gymId,
                cancellationToken);

        return profile?.DisplayRating ?? 0m;
    }

    public async Task<int?> GetMemberLeaderboardPositionAsync(
        Guid memberId,
        Guid gymId,
        string leaderboardType,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<LeaderboardType>(leaderboardType, true, out var type))
        {
            return null;
        }

        var now = DateTime.UtcNow;

        var leaderboard = await _dbContext.MemberLeaderboards
            .AsNoTracking()
            .Include(ml => ml.Entries)
            .Where(ml => ml.GymId == gymId
                && ml.LeaderboardType == type
                && ml.Period.Month == now.Month
                && ml.Period.Year == now.Year)
            .FirstOrDefaultAsync(cancellationToken);

        var entry = leaderboard?.Entries.FirstOrDefault(e => e.MemberId == memberId && e.IsOptedIn);

        return entry?.Rank;
    }

    public async Task<bool> IsCoachOfTheMonthAsync(
        Guid trainerId,
        Guid gymId,
        LeaderboardPeriod period,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrainerMonthlyScores
            .AsNoTracking()
            .AnyAsync(
                tms => tms.TrainerId == trainerId
                    && tms.GymId == gymId
                    && tms.Month == period.Month
                    && tms.Year == period.Year
                    && tms.IsCoachOfTheMonth,
                cancellationToken);
    }
}
