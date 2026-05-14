using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class TrainerRateRepository : ITrainerRateRepository
{
    private readonly GymsDbContext _dbContext;

    public TrainerRateRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TrainerRate?> GetByTrainerAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrainerRates
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.GymId == gymId && r.TrainerId == trainerId, cancellationToken);
    }

    public async Task AddAsync(TrainerRate trainerRate, CancellationToken cancellationToken = default)
    {
        await _dbContext.TrainerRates.AddAsync(trainerRate, cancellationToken);
    }

    public void Update(TrainerRate trainerRate)
    {
        _dbContext.TrainerRates.Update(trainerRate);
    }
}
