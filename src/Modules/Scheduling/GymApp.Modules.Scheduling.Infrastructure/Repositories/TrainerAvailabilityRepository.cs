using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Scheduling.Infrastructure.Repositories;

internal sealed class TrainerAvailabilityRepository : ITrainerAvailabilityRepository
{
    private readonly SchedulingDbContext _dbContext;

    public TrainerAvailabilityRepository(SchedulingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TrainerAvailability?> GetByTrainerAndGymAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrainerAvailabilities
            .Include(a => a.WeeklySlots)
            .Include(a => a.BlockedDates)
            .FirstOrDefaultAsync(
                a => a.TrainerId == trainerId && a.GymId == gymId,
                cancellationToken);
    }

    public async Task AddAsync(
        TrainerAvailability availability,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.TrainerAvailabilities.AddAsync(availability, cancellationToken);
    }

    public void Update(TrainerAvailability availability)
    {
        _dbContext.TrainerAvailabilities.Update(availability);
    }
}
