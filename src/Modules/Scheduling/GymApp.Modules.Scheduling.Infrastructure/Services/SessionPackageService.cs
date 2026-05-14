using GymApp.Modules.Scheduling.Application.Contracts;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Scheduling.Infrastructure.Services;

internal sealed class SessionPackageService : ISessionPackageService
{
    private static readonly SessionStatus[] PendingStatuses =
    {
        SessionStatus.Pending,
        SessionStatus.Confirmed,
        SessionStatus.InProgress
    };

    private readonly SchedulingDbContext _dbContext;

    public SessionPackageService(SchedulingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SessionPackageStatusDto?> GetPackageStatusAsync(
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var package = await _dbContext.SessionPackages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);

        if (package is null)
            return null;

        return new SessionPackageStatusDto(
            package.Id,
            package.MemberId,
            package.Status,
            package.ExpiresAt);
    }

    public async Task<int> GetBookableSessionsAsync(
        Guid memberId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var totalRemaining = await _dbContext.SessionPackages
            .AsNoTracking()
            .Where(p => p.MemberId == memberId
                        && p.GymId == gymId
                        && p.Status == "Active")
            .SumAsync(p => (int?)p.RemainingSessions, cancellationToken) ?? 0;

        var pendingBookings = await _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.MemberId == memberId
                        && s.GymId == gymId
                        && PendingStatuses.Contains(s.Status))
            .CountAsync(cancellationToken);

        var bookable = totalRemaining - pendingBookings;
        return bookable < 0 ? 0 : bookable;
    }
}
