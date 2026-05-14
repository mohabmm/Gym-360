using GymApp.Modules.Nutrition.Domain.Entities;

namespace GymApp.Modules.Nutrition.Domain.Repositories;

public interface IWaterLogRepository
{
    Task<WaterLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WaterLog?> GetByUserAndDateAsync(Guid userId, Guid gymId, DateOnly date, CancellationToken cancellationToken = default);
    Task AddAsync(WaterLog waterLog, CancellationToken cancellationToken = default);
    void Update(WaterLog waterLog);

    // Race-safe atomic insert. Returns the persisted (or already-existing) log
    // for (userId, gymId, date) and appends `amountMl` as a new entry. Implementations
    // must handle SQL Server unique-index collisions internally and retry as needed.
    Task<WaterLog> UpsertAndAppendEntryAsync(
        Guid userId,
        Guid gymId,
        DateOnly date,
        int amountMl,
        CancellationToken cancellationToken = default);
}
