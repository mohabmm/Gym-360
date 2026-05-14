using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Nutrition.Infrastructure.Repositories;

internal sealed class WaterLogRepository : IWaterLogRepository
{
    private readonly NutritionDbContext _dbContext;

    public WaterLogRepository(NutritionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WaterLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WaterLogs
            .Include(w => w.Entries)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<WaterLog?> GetByUserAndDateAsync(
        Guid userId,
        Guid gymId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WaterLogs
            .Include(w => w.Entries)
            .FirstOrDefaultAsync(
                w => w.UserId == userId && w.GymId == gymId && w.Date == date,
                cancellationToken);
    }

    public async Task AddAsync(WaterLog waterLog, CancellationToken cancellationToken = default)
    {
        await _dbContext.WaterLogs.AddAsync(waterLog, cancellationToken);
    }

    public void Update(WaterLog waterLog)
    {
        _dbContext.WaterLogs.Update(waterLog);
    }

    public async Task<WaterLog> UpsertAndAppendEntryAsync(
        Guid userId,
        Guid gymId,
        DateOnly date,
        int amountMl,
        CancellationToken cancellationToken = default)
    {
        // SQL Server unique index IX_water_logs_user_date enforces one row per
        // (UserId, Date). Concurrent first-of-day calls race: each finds no
        // existing row, each Adds, but only one INSERT survives. The losers
        // catch DbUpdateException, detach their failed Added entity, then
        // reload and append on the surviving row.
        const int maxAttempts = 8;
        WaterLog? waterLog = null;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                waterLog = await _dbContext.WaterLogs
                    .Include(w => w.Entries)
                    .FirstOrDefaultAsync(
                        w => w.UserId == userId && w.GymId == gymId && w.Date == date,
                        cancellationToken);

                if (waterLog is null)
                {
                    waterLog = WaterLog.Create(userId, gymId, date);
                    await _dbContext.WaterLogs.AddAsync(waterLog, cancellationToken);
                }

                var addResult = waterLog.AddEntry(amountMl);
                if (addResult.IsFailure)
                    throw new InvalidOperationException(addResult.Error.Name);

                await _dbContext.SaveChangesAsync(cancellationToken);
                return waterLog;
            }
            catch (Exception ex) when (IsRetryableConcurrencyError(ex) && attempt < maxAttempts - 1)
            {
                if (waterLog is not null) DetachLocal(waterLog);
                waterLog = null;
                // Jittered backoff to spread deadlock retries.
                await Task.Delay(Random.Shared.Next(15, 80 * (attempt + 1)), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"WaterLog upsert failed after {maxAttempts} attempts due to concurrent writes.");
    }

    private static bool IsRetryableConcurrencyError(Exception ex)
    {
        if (ex is DbUpdateException) return true;
        // SQL Server deadlock (1205) and lock-request timeout (1222)
        for (var e = (Exception?)ex; e is not null; e = e.InnerException)
            if (e is SqlException sql && (sql.Number == 1205 || sql.Number == 1222))
                return true;
        return false;
    }

    private void DetachLocal(WaterLog waterLog)
    {
        var entry = _dbContext.Entry(waterLog);
        if (entry.State == EntityState.Added)
        {
            entry.State = EntityState.Detached;
            foreach (var e in waterLog.Entries.ToList())
            {
                var ee = _dbContext.Entry(e);
                if (ee.State == EntityState.Added)
                    ee.State = EntityState.Detached;
            }
        }
        else
        {
            // Reload-from-db: detach the stale tracked entity so next loop iteration re-queries.
            entry.State = EntityState.Detached;
        }
    }
}
