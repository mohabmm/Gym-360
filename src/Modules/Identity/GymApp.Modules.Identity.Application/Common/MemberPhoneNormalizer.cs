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
        var trimmed = raw.Trim();
        var digits = new string(trimmed.Where(c => char.IsDigit(c) || c == '+').ToArray());

        if (digits.StartsWith("+")) return digits;
        if (digits.StartsWith("00")) return "+" + digits[2..];
        if (digits.Length == 11 && digits.StartsWith("01")) return "+20" + digits[1..];
        if (digits.Length == 10 && digits.StartsWith("1")) return "+20" + digits;
        return "+" + digits;
    }
}
