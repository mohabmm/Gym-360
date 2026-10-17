using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Domain.ValueObjects;
using GymApp.Modules.Nutrition.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Nutrition.Infrastructure.Repositories;

internal sealed class WaterLogRepository : IWaterLogRepository
{
    private const int MaxUpsertAttempts = 8;
    private const int SqlDeadlockErrorNumber = 1205;
    private const int SqlLockTimeoutErrorNumber = 1222;

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
        WaterIntake intake,
        CancellationToken cancellationToken = default)
    {
        // SQL Server unique index IX_water_logs_user_date enforces one row per
        // (UserId, Date). Concurrent first-of-day calls race: each finds no
        // existing row, each Adds, but only one INSERT survives. The losers
        // catch DbUpdateException, detach their failed Added entity, then
        // reload and append on the surviving row.
        for (var attempt = 0; attempt < MaxUpsertAttempts; attempt++)
        {
            WaterLog? waterLog = null;
            try
            {
                waterLog = await FindOrAddDailyLogAsync(intake, cancellationToken);
                AppendEntry(waterLog, intake.AmountMl);

                await _dbContext.SaveChangesAsync(cancellationToken);
                return waterLog;
            }
            catch (Exception ex) when (IsRetryableConcurrencyError(ex) && attempt < MaxUpsertAttempts - 1)
            {
                if (waterLog is not null) DetachLocal(waterLog);
                // Jittered backoff to spread deadlock retries.
                await Task.Delay(Random.Shared.Next(15, 80 * (attempt + 1)), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"WaterLog upsert failed after {MaxUpsertAttempts} attempts due to concurrent writes.");
    }

    private async Task<WaterLog> FindOrAddDailyLogAsync(WaterIntake intake, CancellationToken cancellationToken)
    {
        var waterLog = await GetByUserAndDateAsync(intake.UserId, intake.GymId, intake.Date, cancellationToken);
        if (waterLog is not null)
            return waterLog;

        waterLog = WaterLog.Create(intake.UserId, intake.GymId, intake.Date);
        await _dbContext.WaterLogs.AddAsync(waterLog, cancellationToken);
        return waterLog;
    }

    private static void AppendEntry(WaterLog waterLog, int amountMl)
    {
        var addResult = waterLog.AddEntry(amountMl);
        if (addResult.IsFailure)
            throw new InvalidOperationException(addResult.Error.Name);
    }

    private static bool IsRetryableConcurrencyError(Exception ex)
    {
        if (ex is DbUpdateException) return true;
        for (var e = (Exception?)ex; e is not null; e = e.InnerException)
            if (IsDeadlockOrLockTimeout(e))
                return true;
        return false;
    }

    // SQL Server deadlock (1205) and lock-request timeout (1222)
    private static bool IsDeadlockOrLockTimeout(Exception ex) =>
        ex is SqlException { Number: SqlDeadlockErrorNumber or SqlLockTimeoutErrorNumber };

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
