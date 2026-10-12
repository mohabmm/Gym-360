using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Application.Contracts;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Scheduling.Infrastructure.Services;

internal sealed class SchedulingQueryService : ISchedulingQueryService
{
    private readonly SchedulingDbContext _dbContext;

    public SchedulingQueryService(SchedulingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> GetCompletedSessionCountAsync(
        Guid trainerId,
        Guid gymId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions
            .AsNoTracking()
            .CountAsync(s => s.TrainerId == trainerId
                && s.GymId == gymId
                && s.Status == SessionStatus.Completed
                && s.ScheduledDate >= from
                && s.ScheduledDate <= to,
                cancellationToken);
    }

    public async Task<int> GetNoShowCountAsync(
        Guid memberId,
        Guid gymId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions
            .AsNoTracking()
            .CountAsync(s => s.MemberId == memberId
                && s.GymId == gymId
                && s.Status == SessionStatus.NoShow
                && s.ScheduledDate >= from
                && s.ScheduledDate <= to,
                cancellationToken);
    }

    public async Task<UpcomingSessionInfo?> GetNextSessionAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var today = GymLocalTime.Today();

        var session = await _dbContext.Sessions
            .AsNoTracking()
            .Where(s => (s.MemberId == userId || s.TrainerId == userId)
                && s.GymId == gymId
                && s.ScheduledDate >= today
                && (s.Status == SessionStatus.Pending || s.Status == SessionStatus.Confirmed))
            .OrderBy(s => s.ScheduledDate)
            .ThenBy(s => s.ScheduledTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
            return null;

        return new UpcomingSessionInfo(
            session.Id,
            session.ScheduledDate,
            session.ScheduledTime,
            session.DurationMinutes,
            session.SessionType.ToString());
    }

    public async Task<int> GetTrainerSessionsForMonthAsync(
        Guid trainerId,
        Guid gymId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _dbContext.Sessions
            .AsNoTracking()
            .CountAsync(s => s.TrainerId == trainerId
                && s.GymId == gymId
                && s.ScheduledDate >= startDate
                && s.ScheduledDate <= endDate
                && s.Status == SessionStatus.Completed,
                cancellationToken);
    }
}
