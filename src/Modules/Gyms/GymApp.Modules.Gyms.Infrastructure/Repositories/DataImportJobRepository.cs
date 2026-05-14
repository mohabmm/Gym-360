using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class DataImportJobRepository : IDataImportJobRepository
{
    private readonly GymsDbContext _dbContext;

    public DataImportJobRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DataImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.DataImportJobs.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DataImportJob>> ListByGymAsync(Guid gymId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DataImportJobs
            .AsNoTracking()
            .Where(j => j.GymId == gymId)
            .OrderByDescending(j => j.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DataImportJob job, CancellationToken cancellationToken = default) =>
        await _dbContext.DataImportJobs.AddAsync(job, cancellationToken);

    public void Update(DataImportJob job) =>
        _dbContext.DataImportJobs.Update(job);
}
