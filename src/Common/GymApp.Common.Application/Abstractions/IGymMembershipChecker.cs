namespace GymApp.Common.Application.Abstractions;

/// <summary>
/// Cross-module contract used by non-Gyms handlers to validate that a caller
/// is an active member of a gym and that the gym is not suspended, without
/// crossing module boundaries into the Gyms domain.
/// Implementation lives in the Gyms module infrastructure.
/// </summary>
public interface IGymMembershipChecker
{
    Task<bool> IsActiveMemberAsync(Guid gymId, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsGymSuspendedAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task<bool> IsMemberFrozenAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
}
