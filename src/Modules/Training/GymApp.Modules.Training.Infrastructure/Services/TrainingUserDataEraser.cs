using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Services;

/// <summary>
/// Deletes the user's workout logs (+sets, cascade) and every assignment / remote config
/// where they are the client or the trainer. Programs and workouts a trainer authored stay
/// as gym content.
/// </summary>
internal sealed class TrainingUserDataEraser : IUserDataEraser
{
    private readonly TrainingDbContext _db;

    public TrainingUserDataEraser(TrainingDbContext db) => _db = db;

    public string ModuleName => "Training";

    public async Task<int> EraseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = 0;
        count += await _db.WorkoutLogs.Where(l => l.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.WorkoutAssignments.Where(a => a.ClientId == userId || a.TrainerId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        count += await _db.RemoteWorkoutConfigs.Where(c => c.ClientId == userId || c.TrainerId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        return count;
    }
}
