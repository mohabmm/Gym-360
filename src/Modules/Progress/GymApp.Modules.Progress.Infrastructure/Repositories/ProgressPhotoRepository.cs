using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Progress.Infrastructure.Repositories;

internal sealed class ProgressPhotoRepository : IProgressPhotoRepository
{
    private readonly ProgressDbContext _dbContext;

    public ProgressPhotoRepository(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProgressPhoto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProgressPhotos
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProgressPhoto>> GetByUserAsync(
        Guid userId,
        Guid gymId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProgressPhotos
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.GymId == gymId)
            .OrderByDescending(e => e.Date)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProgressPhotos
            .AsNoTracking()
            .CountAsync(e => e.UserId == userId && e.GymId == gymId, cancellationToken);
    }

    public async Task AddAsync(ProgressPhoto photo, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProgressPhotos.AddAsync(photo, cancellationToken);
    }

    public void Delete(ProgressPhoto photo)
    {
        _dbContext.ProgressPhotos.Remove(photo);
    }
}
