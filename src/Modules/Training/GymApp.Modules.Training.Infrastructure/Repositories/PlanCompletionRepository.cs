using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Repositories;

internal sealed class PlanCompletionRepository : IPlanCompletionRepository
{
    private readonly TrainingDbContext _db;

    public PlanCompletionRepository(TrainingDbContext db) => _db = db;

    public Task<PlanWorkoutCompletion?> GetByClientIdAsync(Guid traineeId, Guid clientId, CancellationToken cancellationToken = default) =>
        _db.PlanWorkoutCompletions.FirstOrDefaultAsync(c => c.TraineeId == traineeId && c.ClientId == clientId, cancellationToken);

    public async Task<IReadOnlyList<PlanWorkoutCompletion>> ListForPlanAsync(Guid gymId, Guid planId, CancellationToken cancellationToken = default) =>
        await _db.PlanWorkoutCompletions
            .AsNoTracking()
            .Where(c => c.GymId == gymId && c.PlanId == planId)
            .OrderBy(c => c.CompletedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PlanWorkoutCompletion>> ListForSessionAsync(Guid gymId, Guid sessionId, CancellationToken cancellationToken = default) =>
        await _db.PlanWorkoutCompletions
            .Where(c => c.GymId == gymId && c.SessionId == sessionId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PlanWorkoutCompletion>> ListForTraineeAsync(
        Guid gymId, Guid traineeId, DateOnly? since, CancellationToken cancellationToken = default) =>
        await _db.PlanWorkoutCompletions
            .AsNoTracking()
            .Where(c => c.GymId == gymId && c.TraineeId == traineeId && (since == null || c.CompletedOn >= since))
            .OrderByDescending(c => c.CompletedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PlanWorkoutCompletion>> ListForTraineesAsync(
        Guid gymId, IReadOnlyCollection<Guid> traineeIds, DateOnly since, CancellationToken cancellationToken = default) =>
        await _db.PlanWorkoutCompletions
            .AsNoTracking()
            .Where(c => c.GymId == gymId && traineeIds.Contains(c.TraineeId) && c.CompletedOn >= since)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PlanWorkoutCompletion>> ListAllForTraineeAsync(Guid traineeId, CancellationToken cancellationToken = default) =>
        await _db.PlanWorkoutCompletions
            .AsNoTracking()
            .Where(c => c.TraineeId == traineeId)
            .OrderByDescending(c => c.CompletedAt)
            .ToListAsync(cancellationToken);

    public async Task<PlanWorkoutTotals> CountSinceAsync(PlanWorkoutPeriod period, CancellationToken cancellationToken = default)
    {
        var counting = _db.PlanWorkoutCompletions
            .AsNoTracking()
            .Where(c => c.GymId == period.GymId && !c.IsVoided && c.CompletedOn >= period.Since);
        var workouts = await counting.CountAsync(cancellationToken);
        var members = workouts == 0 ? 0 : await counting.Select(c => c.TraineeId).Distinct().CountAsync(cancellationToken);
        return new PlanWorkoutTotals(workouts, members);
    }

    public async Task<bool> TryAddAsync(PlanWorkoutCompletion completion, CancellationToken cancellationToken = default)
    {
        await _db.PlanWorkoutCompletions.AddAsync(completion, cancellationToken);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _db.Entry(completion).State = EntityState.Detached;
            return false;
        }
    }
}
