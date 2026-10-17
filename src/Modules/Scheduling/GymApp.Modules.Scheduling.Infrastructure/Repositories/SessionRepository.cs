using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;
using GymApp.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Scheduling.Infrastructure.Repositories;

internal sealed class SessionRepository : ISessionRepository
{
    private readonly SchedulingDbContext _dbContext;

    public SessionRepository(SchedulingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Session?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions
            .Include(s => s.TrainerLog)
            .Include(s => s.MemberLog)
            .Include(s => s.ExerciseLogs)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetByTrainerAsync(
        Guid trainerId,
        GymDateRange range,
        CancellationToken cancellationToken = default)
    {
        return await InChronologicalOrder(SessionsWithLogsIn(range).Where(s => s.TrainerId == trainerId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetByMemberAsync(
        Guid memberId,
        GymDateRange range,
        CancellationToken cancellationToken = default)
    {
        return await InChronologicalOrder(SessionsWithLogsIn(range).Where(s => s.MemberId == memberId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetUpcomingByTrainerAsync(
        Guid trainerId,
        CancellationToken cancellationToken = default)
    {
        return await InChronologicalOrder(UpcomingActiveSessions().Where(s => s.TrainerId == trainerId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetUpcomingByMemberAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        return await InChronologicalOrder(UpcomingActiveSessions().Where(s => s.MemberId == memberId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetByDateAndTrainerAsync(
        Guid trainerId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.TrainerId == trainerId && s.ScheduledDate == date)
            .OrderBy(s => s.ScheduledTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Session session,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Sessions.AddAsync(session, cancellationToken);
    }

    public void Update(Session session)
    {
        _dbContext.Sessions.Update(session);
    }

    public async Task<int> GetCountByStatusAsync(
        Guid trainerId,
        SessionStatus status,
        GymDateRange range,
        CancellationToken cancellationToken = default)
    {
        return await SessionsIn(range)
            .AsNoTracking()
            .CountAsync(s => s.TrainerId == trainerId && s.Status == status, cancellationToken);
    }

    private IQueryable<Session> SessionsIn(GymDateRange range) =>
        _dbContext.Sessions
            .Where(s => s.GymId == range.GymId)
            .Where(s => s.ScheduledDate >= range.From && s.ScheduledDate <= range.To);

    private IQueryable<Session> SessionsWithLogsIn(GymDateRange range) =>
        SessionsIn(range)
            .AsNoTracking()
            .Include(s => s.TrainerLog)
            .Include(s => s.MemberLog);

    private IQueryable<Session> UpcomingActiveSessions()
    {
        var today = GymLocalTime.Today();

        return _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.ScheduledDate >= today)
            .Where(s => s.Status != SessionStatus.CancelledByTrainer
                && s.Status != SessionStatus.CancelledByMember
                && s.Status != SessionStatus.LateCancelled
                && s.Status != SessionStatus.NoShow);
    }

    private static IOrderedQueryable<Session> InChronologicalOrder(IQueryable<Session> sessions) =>
        sessions
            .OrderBy(s => s.ScheduledDate)
            .ThenBy(s => s.ScheduledTime);
}
