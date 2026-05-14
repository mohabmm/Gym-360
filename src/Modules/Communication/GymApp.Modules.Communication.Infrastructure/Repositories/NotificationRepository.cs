using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Communication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Communication.Infrastructure.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly CommunicationDbContext _dbContext;

    public NotificationRepository(CommunicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<List<Notification>> GetByUserAsync(
        Guid userId,
        Guid? gymId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (gymId.HasValue)
        {
            query = query.Where(n => n.GymId == gymId.Value || n.GymId == null);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        Guid? gymId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead);

        if (gymId.HasValue)
        {
            query = query.Where(n => n.GymId == gymId.Value || n.GymId == null);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
    }

    public void Update(Notification notification)
    {
        _dbContext.Notifications.Update(notification);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, DateTime.UtcNow),
                cancellationToken);
    }
}
