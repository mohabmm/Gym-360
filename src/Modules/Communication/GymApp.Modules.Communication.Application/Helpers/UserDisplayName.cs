using GymApp.Common.Application.Abstractions;

namespace GymApp.Modules.Communication.Application.Helpers;

internal static class UserDisplayName
{
    /// Resolves a non-empty display name for a known user profile, preferring
    /// "FirstName LastName" and falling back to the email's local-part. Empty
    /// string only when the profile carries no usable identity at all (rare
    /// — typically a deleted-but-referenced account).
    public static string Resolve(UserProfileInfo profile)
    {
        var fullName = $"{profile.FirstName} {profile.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName)) return fullName;

        if (!string.IsNullOrWhiteSpace(profile.Email))
        {
            var at = profile.Email.IndexOf('@');
            return at > 0 ? profile.Email[..at] : profile.Email;
        }

        return string.Empty;
    }
}
