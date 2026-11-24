using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Repositories;

internal sealed class PlanProgramUnlockRepository : IPlanProgramUnlockRepository
{
    private readonly TrainingDbContext _db;

    public PlanProgramUnlockRepository(TrainingDbContext db) => _db = db;

    public Task<PlanProgramUnlock?> GetAsync(CoachTrainee pair, CancellationToken cancellationToken = default) =>
        _db.PlanProgramUnlocks.FirstOrDefaultAsync(
            u => u.GymId == pair.GymId && u.TrainerId == pair.TrainerId && u.TraineeId == pair.TraineeId,
            cancellationToken);

    public async Task<UnlockLength?> GetOpenLengthAsync(CoachTrainee pair, CancellationToken cancellationToken = default) =>
        await _db.PlanProgramUnlocks
            .AsNoTracking()
            .Where(u => u.GymId == pair.GymId && u.TrainerId == pair.TrainerId && u.TraineeId == pair.TraineeId
                        && u.LockedAt == null)
            .Select(u => (UnlockLength?)u.Length)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<PlanProgramUnlock>> ListForTraineeAsync(
        Guid gymId, Guid traineeId, CancellationToken cancellationToken = default) =>
        await _db.PlanProgramUnlocks
            .AsNoTracking()
            .Where(u => u.GymId == gymId && u.TraineeId == traineeId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PlanProgramUnlock unlock, CancellationToken cancellationToken = default) =>
        await _db.PlanProgramUnlocks.AddAsync(unlock, cancellationToken);
}
