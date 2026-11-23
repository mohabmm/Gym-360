namespace GymApp.Modules.Gyms.Shared;

/// <summary>
/// When a member joined the gym roster (UTC), for features that count from the join date
/// (mem.first_month_challenge). Null when the member has no roster row at this gym.
/// Implementation lives in the Gyms module infrastructure.
/// </summary>
public interface IGymMemberJoinDateLookup
{
    Task<DateTime?> GetJoinedAtAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
}
