using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Services;

internal sealed class TrainerAssignmentLookup : ITrainerAssignmentLookup
{
    private readonly BillingDbContext _dbContext;

    public TrainerAssignmentLookup(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, Guid>> GetAssignedTrainersAsync(
        Guid memberId,
        IEnumerable<Guid> gymIds,
        CancellationToken cancellationToken = default)
    {
        var gymList = gymIds.Distinct().ToList();
        if (gymList.Count == 0)
            return new Dictionary<Guid, Guid>();

        var rows = await _dbContext.SessionPackages
            .AsNoTracking()
            .Where(p => p.MemberId == memberId
                        && gymList.Contains(p.GymId)
                        && p.Status == PackageStatus.Active)
            .GroupBy(p => p.GymId)
            .Select(g => new
            {
                GymId = g.Key,
                TrainerId = g.OrderByDescending(p => p.CreatedAt)
                             .Select(p => p.TrainerId)
                             .First()
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.GymId, r => r.TrainerId);
    }

    public async Task<IReadOnlyList<Guid>> GetClientsForTrainerAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SessionPackages
            .AsNoTracking()
            .Where(p => p.TrainerId == trainerId
                        && p.GymId == gymId
                        && p.Status == PackageStatus.Active)
            .Select(p => p.MemberId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
