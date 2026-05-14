namespace GymApp.Modules.Gyms.Application.Contracts;

/// <summary>
/// Read/write access to per-gym member metadata (MemberNumber, Section).
/// MemberNumbers are unique within a gym and used by reception-desk flows
/// such as check-in-by-number.
/// </summary>
public interface IGymMemberDirectory
{
    /// <summary>Resolves a memberId from a human-facing member number at the given gym.</summary>
    Task<Guid?> FindMemberIdByNumberAsync(Guid gymId, string memberNumber, CancellationToken cancellationToken);

    /// <summary>Generates and registers a new member number for the given (gymId, memberId).</summary>
    /// <param name="preferredNumber">Optional caller-supplied number. Must be unique within the gym.</param>
    /// <returns>The assigned MemberNumber.</returns>
    Task<string> RegisterAsync(Guid gymId, Guid memberId, string? preferredNumber, string? section, CancellationToken cancellationToken);

    /// <summary>Returns the current MemberNumber for (gymId, memberId), or null if not registered.</summary>
    Task<string?> GetMemberNumberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken);
}
