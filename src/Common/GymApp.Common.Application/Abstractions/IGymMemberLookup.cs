namespace GymApp.Common.Application.Abstractions;

/// <summary>
/// Cross-module contract to resolve a gym-scoped member number to a user id.
/// Used by data-import event handlers in Billing / Progress to associate
/// imported rows with the user created by the Identity handler.
/// Implementation lives in the Identity module infrastructure.
/// </summary>
public interface IGymMemberLookup
{
    Task<Guid?> GetUserIdByMemberNumberAsync(
        Guid gymId,
        string memberNumber,
        CancellationToken cancellationToken = default);
}
