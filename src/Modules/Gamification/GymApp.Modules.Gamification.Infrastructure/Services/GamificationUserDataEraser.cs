using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gamification.Infrastructure.Services;

/// <summary>
/// Deletes leaderboard rows and opt-outs, ratings the user gave or received, and a
/// trainer's monthly scores and public profile (testimonials cascade).
/// </summary>
internal sealed class GamificationUserDataEraser : IUserDataEraser
{
    private readonly GamificationDbContext _db;

    public GamificationUserDataEraser(GamificationDbContext db) => _db = db;

    public string ModuleName => "Gamification";

    public async Task<int> EraseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = 0;
        count += await _db.LeaderboardEntries.Where(e => e.MemberId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.LeaderboardOptOuts.Where(o => o.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.TrainerRatings.Where(r => r.ClientId == userId || r.TrainerId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        count += await _db.TrainerMonthlyScores.Where(s => s.TrainerId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.TrainerPublicProfiles.Where(p => p.TrainerId == userId).ExecuteDeleteAsync(cancellationToken);
        return count;
    }
}
