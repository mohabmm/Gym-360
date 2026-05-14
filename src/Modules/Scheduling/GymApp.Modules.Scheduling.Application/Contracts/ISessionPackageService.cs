namespace GymApp.Modules.Scheduling.Application.Contracts;

public interface ISessionPackageService
{
    /// <summary>
    /// Returns the package status and expiry date for the given package,
    /// or null if the package does not exist.
    /// </summary>
    Task<SessionPackageStatusDto?> GetPackageStatusAsync(
        Guid packageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bookable sessions across the member's Active packages at the gym:
    /// sum(remainingSessions) − count(member's not-yet-charged sessions:
    /// Pending / Confirmed / InProgress). Used by booking to enforce capacity
    /// even when the trainer hasn't accepted the request yet.
    /// </summary>
    Task<int> GetBookableSessionsAsync(
        Guid memberId,
        Guid gymId,
        CancellationToken cancellationToken = default);
}

public sealed record SessionPackageStatusDto(
    Guid Id,
    Guid MemberId,
    string Status,
    DateTime? ExpiresAt)
{
    public bool IsExpired => Status == "Expired"
                             || (Status == "Active" && ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow);
}
