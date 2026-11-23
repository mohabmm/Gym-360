using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Domain.Repositories;

public interface IChallengeAwardRepository
{
    /// <summary>The member's award at this gym, if they reached the target (read-only).</summary>
    Task<ChallengeAward?> GetForTraineeAsync(Guid gymId, Guid traineeId, CancellationToken cancellationToken = default);

    /// <summary>Tracked, for the desk's "delivered" tick.</summary>
    Task<ChallengeAward?> GetByIdAsync(Guid gymId, Guid awardId, CancellationToken cancellationToken = default);

    /// <summary>The gym's awards, newest first; <paramref name="delivered"/> null = all (read-only).</summary>
    Task<IReadOnlyList<ChallengeAward>> ListAsync(Guid gymId, bool? delivered, CancellationToken cancellationToken = default);

    /// <summary>Every award of the member in any gym of this scope (data export).</summary>
    Task<IReadOnlyList<ChallengeAward>> ListAllForTraineeAsync(Guid traineeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the award together with whatever else is pending (its notification event).
    /// False when the member already has one (the unique index rejected it); nothing is saved then.
    /// </summary>
    Task<bool> TryAddAsync(ChallengeAward award, CancellationToken cancellationToken = default);
}
