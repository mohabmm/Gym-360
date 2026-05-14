using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Repositories;

internal sealed class RemoteWorkoutConfigRepository : IRemoteWorkoutConfigRepository
{
    private readonly TrainingDbContext _dbContext;

    public RemoteWorkoutConfigRepository(TrainingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RemoteWorkoutConfig?> GetByTrainerAndClientAsync(
        Guid trainerId,
        Guid clientId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RemoteWorkoutConfigs
            .FirstOrDefaultAsync(rwc =>
                rwc.TrainerId == trainerId &&
                rwc.ClientId == clientId &&
                rwc.GymId == gymId,
                cancellationToken);
    }

    public async Task<RemoteWorkoutConfig?> GetActiveByClientAsync(
        Guid clientId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RemoteWorkoutConfigs
            .AsNoTracking()
            .Where(rwc =>
                rwc.ClientId == clientId &&
                rwc.GymId == gymId &&
                rwc.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(RemoteWorkoutConfig config, CancellationToken cancellationToken = default)
    {
        await _dbContext.RemoteWorkoutConfigs.AddAsync(config, cancellationToken);
    }

    public void Update(RemoteWorkoutConfig config)
    {
        _dbContext.RemoteWorkoutConfigs.Update(config);
    }
}
