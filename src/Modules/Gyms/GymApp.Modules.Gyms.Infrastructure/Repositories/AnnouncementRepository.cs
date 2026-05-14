using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class AnnouncementRepository : IAnnouncementRepository
{
    private readonly GymsDbContext _dbContext;

    public AnnouncementRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Announcement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Announcements
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<List<Announcement>> GetByGymIdAsync(Guid gymId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Announcements
            .AsNoTracking()
            .Where(a => a.GymId == gymId)
            .OrderByDescending(a => a.PublishedAt ?? a.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Announcement announcement, CancellationToken cancellationToken = default)
    {
        await _dbContext.Announcements.AddAsync(announcement, cancellationToken);
    }
}
