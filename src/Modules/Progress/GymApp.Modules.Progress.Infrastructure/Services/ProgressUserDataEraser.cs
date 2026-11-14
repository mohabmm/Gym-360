using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Progress.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Progress.Infrastructure.Services;

/// <summary>Deletes all of the user's body metrics, progress photos, streaks and goals.</summary>
internal sealed class ProgressUserDataEraser : IUserDataEraser
{
    private readonly ProgressDbContext _db;

    public ProgressUserDataEraser(ProgressDbContext db) => _db = db;

    public string ModuleName => "Progress";

    public async Task<int> EraseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = 0;
        count += await _db.BodyMetricEntries.Where(e => e.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.ProgressPhotos.Where(p => p.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.Streaks.Where(s => s.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.ProgressGoals.Where(g => g.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        return count;
    }
}
