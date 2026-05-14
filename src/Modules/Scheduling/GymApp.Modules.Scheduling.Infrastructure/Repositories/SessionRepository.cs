using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Repositories;
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
        Guid gymId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.TrainerLog)
            .Include(s => s.MemberLog)
            .Where(s => s.TrainerId == trainerId
                && s.GymId == gymId
                && s.ScheduledDate >= startDate
                && s.ScheduledDate <= endDate)
            .OrderBy(s => s.ScheduledDate)
            .ThenBy(s => s.ScheduledTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetByMemberAsync(
        Guid memberId,
        Guid gymId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.TrainerLog)
            .Include(s => s.MemberLog)
            .Where(s => s.MemberId == memberId
                && s.GymId == gymId
                && s.ScheduledDate >= startDate
                && s.ScheduledDate <= endDate)
            .OrderBy(s => s.ScheduledDate)
            .ThenBy(s => s.ScheduledTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetUpcomingByTrainerAsync(
        Guid trainerId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.TrainerId == trainerId
                && s.ScheduledDate >= today
                && s.Status != SessionStatus.CancelledByTrainer
                && s.Status != SessionStatus.CancelledByMember
                && s.Status != SessionStatus.LateCancelled
                && s.Status != SessionStatus.NoShow)
            .OrderBy(s => s.ScheduledDate)
            .ThenBy(s => s.ScheduledTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetUpcomingByMemberAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.MemberId == memberId
                && s.ScheduledDate >= today
                && s.Status != SessionStatus.CancelledByTrainer
                && s.Status != SessionStatus.CancelledByMember
                && s.Status != SessionStatus.LateCancelled
                && s.Status != SessionStatus.NoShow)
            .OrderBy(s => s.ScheduledDate)
            .ThenBy(s => s.ScheduledTime)
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
        Guid gymId,
        SessionStatus status,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions
            .AsNoTracking()
            .CountAsync(s => s.TrainerId == trainerId
                && s.GymId == gymId
                && s.Status == status
                && s.ScheduledDate >= startDate
                && s.ScheduledDate <= endDate,
                cancellationToken);
    }
}
