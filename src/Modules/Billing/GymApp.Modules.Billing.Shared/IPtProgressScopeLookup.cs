namespace GymApp.Modules.Billing.Shared;

/// <summary>
/// pt.exercise_progress: what a member's PT history looks like from Billing's side, so the
/// progress board can label trial sessions and show "6 of 20 trial sessions". Implementation
/// lives in Billing.
/// </summary>
public interface IPtProgressScopeLookup
{
    Task<PtProgressScope> GetScopeAsync(
        Guid memberId,
        Guid gymId,
        CancellationToken cancellationToken = default);
}

/// <param name="TrialSessionIds">Sessions counted against the member's free trial (reversed ones excluded).</param>
/// <param name="TrialSessionsTotal">Sessions the member's trial packages offered in total (0 = no trial).</param>
/// <param name="HasPaidPackage">The member has had a paid PT package in this gym, whatever its status now.</param>
public sealed record PtProgressScope(
    IReadOnlyCollection<Guid> TrialSessionIds,
    int TrialSessionsTotal,
    bool HasPaidPackage)
{
    public static PtProgressScope None { get; } = new(Array.Empty<Guid>(), 0, false);
}
