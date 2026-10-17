using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Modules.Progress.Domain.Repositories;

public interface IBodyMetricEntryRepository
{
    Task<BodyMetricEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BodyMetricEntry>> GetByUserAsync(
        Guid userId,
        Guid gymId,
        DateRange dates,
        CancellationToken cancellationToken = default);

    Task<BodyMetricEntry?> GetByUserAndDateAsync(
        Guid userId,
        Guid gymId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<BodyMetricEntry?> GetLatestAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default);

    Task AddAsync(BodyMetricEntry entry, CancellationToken cancellationToken = default);

    void Update(BodyMetricEntry entry);
}
