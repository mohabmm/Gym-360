using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class GymTrainerRequestRepository : IGymTrainerRequestRepository
{
    private readonly GymsDbContext _dbContext;

    public GymTrainerRequestRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GymTrainerRequest?> GetAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default)
        => await _dbContext.GymTrainerRequests
            .FirstOrDefaultAsync(r => r.GymId == gymId && r.TrainerId == trainerId, cancellationToken);

    public async Task AddAsync(GymTrainerRequest request, CancellationToken cancellationToken = default)
        => await _dbContext.GymTrainerRequests.AddAsync(request, cancellationToken);

    public void Update(GymTrainerRequest request)
        => _dbContext.GymTrainerRequests.Update(request);
}
