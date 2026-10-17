using System.Text.RegularExpressions;

namespace GymApp.Api.Orchestration;

/// <summary>
/// Server-side scrub + truncation for <see cref="ClientLogsEndpoint"/> records (defense in
/// depth on top of client-side redaction): no auth/token/password/body fields are persisted.
/// </summary>
internal static class ClientLogScrubber
{
    public const int MaxMessageLength = 4000;
    public const int MaxStackLength = 16000;
    public const int MaxErrorTypeLength = 200;
    public const int MaxAttrValueLength = 1000;

    // Attribute keys we refuse to forward even if the client sends them.
    private static readonly HashSet<string> BlockedAttrKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization", "password", "token", "accesstoken", "refreshtoken",
        "access_token", "refresh_token", "email", "phone", "secret",
        "body", "requestbody", "responsebody", "data",
    };

    // Free-text fields (message, stack, attribute values) can embed PII even
    // though the client scrubs structured fields. Redact emails, long digit runs
    // (phone numbers), and bearer/JWT tokens server-side too (PDPL / Rule #9).
    private static readonly Regex EmailRx =
        new(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.Compiled);
    private static readonly Regex PhoneRx =
        new(@"\+?\d[\d\s\-]{8,}\d", RegexOptions.Compiled);
    private static readonly Regex TokenRx =
        new(@"(?i)bearer\s+[A-Za-z0-9._\-]+|eyJ[A-Za-z0-9._\-]{10,}", RegexOptions.Compiled);

    public static string Redact(string s)
    {
        s = EmailRx.Replace(s, "[email]");
        s = TokenRx.Replace(s, "[token]");
        s = PhoneRx.Replace(s, "[phone]");
        return s;
    }

    public static bool IsBlockedAttribute(string? key) =>
        string.IsNullOrEmpty(key) || BlockedAttrKeys.Contains(key);

    public static string ScrubAttributeValue(object? value) => value switch
    {
        string s => Truncate(Redact(s), MaxAttrValueLength),
        null => "",
        _ => Truncate(Redact(value.ToString() ?? ""), MaxAttrValueLength),
    };

    public static string? Sanitize(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : Truncate(s.Trim(), MaxAttrValueLength);

    public static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max];
}
