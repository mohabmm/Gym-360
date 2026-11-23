using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Repositories;

internal sealed class TrainingPlanRepository : ITrainingPlanRepository
{
    private readonly TrainingDbContext _db;

    public TrainingPlanRepository(TrainingDbContext db) => _db = db;

    public Task<TrainingPlan?> GetByIdAsync(Guid gymId, Guid planId, CancellationToken cancellationToken = default) =>
        _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == planId && p.GymId == gymId, cancellationToken);

    public Task<TrainingPlan?> GetActiveForTraineeAsync(Guid gymId, Guid traineeId, CancellationToken cancellationToken = default) =>
        _db.TrainingPlans.FirstOrDefaultAsync(
            p => p.GymId == gymId && p.TraineeId == traineeId && p.Status == TrainingPlanStatus.Active, cancellationToken);

    public async Task<IReadOnlyList<TrainingPlan>> ListForTraineeAsync(Guid gymId, Guid traineeId, CancellationToken cancellationToken = default) =>
        await _db.TrainingPlans
            .AsNoTracking()
            .Where(p => p.GymId == gymId && p.TraineeId == traineeId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TrainingPlan>> ListActiveForTraineesAsync(
        Guid gymId, IReadOnlyCollection<Guid> traineeIds, CancellationToken cancellationToken = default) =>
        await _db.TrainingPlans
            .AsNoTracking()
            .Where(p => p.GymId == gymId && p.Status == TrainingPlanStatus.Active && traineeIds.Contains(p.TraineeId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TrainingPlan>> ListAllForTraineeAsync(Guid traineeId, CancellationToken cancellationToken = default) =>
        await _db.TrainingPlans
            .AsNoTracking()
            .Where(p => p.TraineeId == traineeId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TrainingPlan plan, CancellationToken cancellationToken = default) =>
        await _db.TrainingPlans.AddAsync(plan, cancellationToken);
}
