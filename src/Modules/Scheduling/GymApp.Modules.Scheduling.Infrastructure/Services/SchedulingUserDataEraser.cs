using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;
using GymApp.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Scheduling.Infrastructure.Services;

/// <summary>
/// Member: their sessions are deleted (trainer/member/exercise logs cascade).
/// Trainer: availability is deleted (slots + blocked dates cascade) and upcoming sessions
/// are cancelled through the domain so members get the normal cancellation side effects;
/// past sessions stay in the members' history.
/// </summary>
internal sealed class SchedulingUserDataEraser : IUserDataEraser
{
    private const string CancellationReason = "Trainer account deleted";

    private readonly SchedulingDbContext _db;
    private readonly ISessionRepository _sessions;

    public SchedulingUserDataEraser(SchedulingDbContext db, ISessionRepository sessions)
    {
        _db = db;
        _sessions = sessions;
    }

    public string ModuleName => "Scheduling";

    public async Task<int> EraseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = await CancelUpcomingAsTrainerAsync(userId, cancellationToken);
        count += await _db.Sessions.Where(s => s.MemberId == userId).ExecuteDeleteAsync(cancellationToken);
        count += await _db.TrainerAvailabilities.Where(a => a.TrainerId == userId).ExecuteDeleteAsync(cancellationToken);
        return count;
    }

    private async Task<int> CancelUpcomingAsTrainerAsync(Guid trainerId, CancellationToken cancellationToken)
    {
        var upcoming = await _sessions.GetUpcomingByTrainerAsync(trainerId, cancellationToken);
        var cancellation = new SessionCancellation(CancellationReason, trainerId, IsLateCancellation: false);
        var cancelled = upcoming.Count(session => session.Cancel(cancellation).IsSuccess);

        if (cancelled > 0)
            await _db.SaveChangesAsync(cancellationToken);
        return cancelled;
    }
}
