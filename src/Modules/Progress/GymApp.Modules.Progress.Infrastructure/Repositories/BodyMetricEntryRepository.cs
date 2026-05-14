using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Progress.Infrastructure.Repositories;

internal sealed class BodyMetricEntryRepository : IBodyMetricEntryRepository
{
    private readonly ProgressDbContext _dbContext;

    public BodyMetricEntryRepository(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BodyMetricEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.BodyMetricEntries
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<BodyMetricEntry>> GetByUserAsync(
        Guid userId,
        Guid gymId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.BodyMetricEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.GymId == gymId && e.Date >= fromDate && e.Date <= toDate)
            .OrderByDescending(e => e.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<BodyMetricEntry?> GetByUserAndDateAsync(
        Guid userId,
        Guid gymId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.BodyMetricEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.GymId == gymId && e.Date == date, cancellationToken);
    }

    public async Task<BodyMetricEntry?> GetLatestAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.BodyMetricEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.GymId == gymId)
            .OrderByDescending(e => e.Date)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(BodyMetricEntry entry, CancellationToken cancellationToken = default)
    {
        await _dbContext.BodyMetricEntries.AddAsync(entry, cancellationToken);
    }

    public void Update(BodyMetricEntry entry)
    {
        _dbContext.BodyMetricEntries.Update(entry);
    }
}
