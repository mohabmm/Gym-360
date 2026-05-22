namespace GymApp.Modules.Billing.Shared;

/// <summary>
/// Resolves the trainer currently assigned to a trainee, per gym. "Assigned"
/// means the trainer on the member's newest Active session package in that gym.
/// Returned as a gymId → trainerId map so callers can resolve multiple
/// memberships in a single round trip. Implementation lives in Billing.
/// </summary>
public interface ITrainerAssignmentLookup
{
    Task<IReadOnlyDictionary<Guid, Guid>> GetAssignedTrainersAsync(
        Guid memberId,
        IEnumerable<Guid> gymIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverse of <see cref="GetAssignedTrainersAsync"/>: returns the distinct
    /// MemberIds with an Active session package booked against this trainer
    /// in the given gym. Empty when the trainer has no live clients.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetClientsForTrainerAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default);
}
