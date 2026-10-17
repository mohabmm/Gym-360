namespace GymApp.Modules.Identity.Shared;

/// <summary>
/// Optional profile field changes for <see cref="IUserProfileUpdater"/>. A null field
/// means "leave unchanged".
/// </summary>
public sealed record UserProfileChanges(string? FirstName, string? LastName, string? Phone)
{
    /// <summary>True when at least one field is set (i.e. there is something to update).</summary>
    public bool HasAny => FirstName is not null || LastName is not null || Phone is not null;
}
