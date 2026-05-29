using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Infrastructure.Data;
using GymApp.Modules.Billing.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Repositories;

internal sealed class TrainerEarningsRepository : ITrainerEarningsRepository
{
    private readonly BillingDbContext _dbContext;

    public TrainerEarningsRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TrainerEarningsRecord>> GetByTrainerAsync(
        Guid gymId,
        Guid trainerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrainerEarningsRecords
            .AsNoTracking()
            .Where(r => r.GymId == gymId
                     && r.TrainerId == trainerId
                     && r.EarnedAt >= from
                     && r.EarnedAt <= to)
            .OrderByDescending(r => r.EarnedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TrainerEarningsRecord record, CancellationToken cancellationToken = default)
    {
        await _dbContext.TrainerEarningsRecords.AddAsync(record, cancellationToken);
    }

    public async Task<bool> ExistsForSessionAsync(Guid gymId, Guid trainerId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrainerEarningsRecords
            .AsNoTracking()
            .AnyAsync(r => r.GymId == gymId && r.TrainerId == trainerId && r.SessionId == sessionId, cancellationToken);
    }

    public async Task<decimal?> GetTrainerRateAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default)
    {
        var readModel = await _dbContext.TrainerRates
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.GymId == gymId && r.TrainerId == trainerId, cancellationToken);

        return readModel?.RatePerSession;
    }
}
