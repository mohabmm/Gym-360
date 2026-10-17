namespace GymApp.Modules.Identity.Application.Common;

/// <summary>
/// Normalizes staff-entered phone numbers to E.164 before lookup/creation so that
/// "01001596189", "+201001596189" and "00201001596189" all resolve to the same
/// stored value. Shared by staff-create, staff-lookup and staff-link-existing so a
/// returning member is matched regardless of which format reception typed.
/// </summary>
internal static class MemberPhoneNormalizer
{
    public static string Normalize(string raw)
    {
        // Strip spaces, dashes, parentheses.
        var digits = new string(raw.Trim().Where(c => char.IsDigit(c) || c == '+').ToArray());

        // Already E.164.
        if (digits.StartsWith("+")) return digits;

        // "00" international prefix -> "+"
        if (digits.StartsWith("00")) return "+" + digits[2..];

        return ToEgyptianOrGenericE164(digits);
    }

    /// <summary>
    /// Like <see cref="Normalize"/>, but an empty/missing phone yields a deterministic
    /// placeholder derived from the email so retries don't collide and the same user
    /// always gets the same synthetic number.
    /// </summary>
    public static string NormalizeOrPlaceholder(string? raw, string email)
    {
        var trimmed = raw?.Trim() ?? string.Empty;

        return string.IsNullOrEmpty(trimmed)
            ? PlaceholderFor(email)
            : Normalize(trimmed);
    }

    private static string ToEgyptianOrGenericE164(string digits)
    {
        // Egyptian local: 11 digits starting "01" (mobile) -> "+20" + last 10 digits.
        if (digits.Length == 11 && digits.StartsWith("01")) return "+20" + digits[1..];

        // 10 digits starting "1" (mobile minus the leading 0) -> "+20" + digits.
        if (digits.Length == 10 && digits.StartsWith("1")) return "+20" + digits;

        // Fallback: prepend + and let the VO validate.
        return "+" + digits;
    }

    private static string PlaceholderFor(string email)
    {
        var hash = Math.Abs(email.GetHashCode()).ToString().PadLeft(10, '0');
        return "+999" + hash[..Math.Min(10, hash.Length)];
    }
}
