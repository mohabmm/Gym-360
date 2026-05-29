using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface ITrainerEarningsRepository
{
    Task<IReadOnlyList<TrainerEarningsRecord>> GetByTrainerAsync(
        Guid gymId,
        Guid trainerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task AddAsync(TrainerEarningsRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if an earnings record already exists for this trainer + session. Keeps SessionCompleted
    /// handling idempotent under at-least-once outbox delivery (no duplicate trainer payout).
    /// </summary>
    Task<bool> ExistsForSessionAsync(Guid gymId, Guid trainerId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cross-module read: returns the current rate per session configured in gyms.trainer_rates.
    /// Returns null if no rate has been configured.
    /// </summary>
    Task<decimal?> GetTrainerRateAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default);
}
