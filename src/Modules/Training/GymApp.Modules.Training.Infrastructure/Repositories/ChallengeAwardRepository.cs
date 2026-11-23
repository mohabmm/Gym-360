using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Repositories;

internal sealed class ChallengeAwardRepository : IChallengeAwardRepository
{
    private readonly TrainingDbContext _db;

    public ChallengeAwardRepository(TrainingDbContext db) => _db = db;

    public Task<ChallengeAward?> GetForTraineeAsync(Guid gymId, Guid traineeId, CancellationToken cancellationToken = default) =>
        _db.ChallengeAwards.AsNoTracking().FirstOrDefaultAsync(a => a.GymId == gymId && a.TraineeId == traineeId, cancellationToken);

    public Task<ChallengeAward?> GetByIdAsync(Guid gymId, Guid awardId, CancellationToken cancellationToken = default) =>
        _db.ChallengeAwards.FirstOrDefaultAsync(a => a.GymId == gymId && a.Id == awardId, cancellationToken);

    public async Task<IReadOnlyList<ChallengeAward>> ListAsync(Guid gymId, bool? delivered, CancellationToken cancellationToken = default) =>
        await _db.ChallengeAwards
            .AsNoTracking()
            .Where(a => a.GymId == gymId && (delivered == null || (a.DeliveredAt != null) == delivered))
            .OrderByDescending(a => a.ReachedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ChallengeAward>> ListAllForTraineeAsync(Guid traineeId, CancellationToken cancellationToken = default) =>
        await _db.ChallengeAwards.AsNoTracking().Where(a => a.TraineeId == traineeId).ToListAsync(cancellationToken);

    public async Task<bool> TryAddAsync(ChallengeAward award, CancellationToken cancellationToken = default)
    {
        await _db.ChallengeAwards.AddAsync(award, cancellationToken);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Drop the award and the outbox row of its announcement, so neither is retried later.
            foreach (var entry in _db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList())
                entry.State = EntityState.Detached;
            return false;
        }
    }
}
