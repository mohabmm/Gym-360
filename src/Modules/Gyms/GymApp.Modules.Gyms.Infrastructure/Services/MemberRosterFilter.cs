namespace GymApp.Modules.Gyms.Infrastructure.Services;

/// <summary>
/// Parsed status / section / search filters for the gym member roster, plus the
/// parts of the filtering that only need the gym_members directory.
/// </summary>
internal sealed record MemberRosterFilter(
    string? Section, string? Search, string? ProfileStatus, bool FrozenOnly, bool IsStatusSupported)
{
    public bool HasSection => !string.IsNullOrWhiteSpace(Section);

    public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

    public bool NeedsProfileLookup => HasSearch || ProfileStatus is not null;

    // The directory is needed for section / frozen / member-number search, and also
    // to exclude frozen members from an "active"/"inactive" filter (frozen wins in the UI).
    public bool NeedsDirectory => HasSection || FrozenOnly || NeedsProfileLookup;

    public static MemberRosterFilter Parse(string? status, string? section, string? search)
    {
        var statusKey = status?.Trim().ToLowerInvariant();

        return new MemberRosterFilter(
            section,
            search,
            // Only "active"/"inactive" map to an Identity profile status; "frozen" is a gym
            // directory flag (handled here).
            ProfileStatus: statusKey is "active" or "inactive" ? statusKey : null,
            FrozenOnly: statusKey == "frozen",
            // Any other status value ("expired", typos) has no backing data.
            IsStatusSupported: statusKey is null or "active" or "inactive" or "frozen");
    }

    public IEnumerable<Guid> ApplyDirectoryFilters(
        IEnumerable<Guid> memberIds, IReadOnlyDictionary<Guid, MemberDirectoryEntry> directory)
    {
        if (HasSection)
            memberIds = memberIds.Where(id =>
                directory.TryGetValue(id, out var d) &&
                string.Equals(d.Section, Section, StringComparison.OrdinalIgnoreCase));

        if (FrozenOnly)
            return memberIds.Where(id => IsFrozen(directory, id));

        // A frozen member always displays as "frozen", never active/inactive.
        return ProfileStatus is null
            ? memberIds
            : memberIds.Where(id => !IsFrozen(directory, id));
    }

    // Member number lives in the Gyms directory, not the Identity profile, so the
    // search term is matched against it here.
    public List<Guid> MatchMemberNumbers(
        IEnumerable<Guid> memberIds, IReadOnlyDictionary<Guid, MemberDirectoryEntry> directory)
    {
        var term = Search!.Trim();
        return memberIds
            .Where(id => directory.TryGetValue(id, out var d) &&
                         d.MemberNumber is not null &&
                         d.MemberNumber.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static bool IsFrozen(IReadOnlyDictionary<Guid, MemberDirectoryEntry> directory, Guid memberId)
        => directory.TryGetValue(memberId, out var d) && d.IsFrozen;
}
