using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using GymApp.Common.Presentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace GymApp.Api.Orchestration;

/// <summary>
/// Ingest endpoint for client (mobile-app) telemetry. The three Flutter apps batch
/// their logs + uncaught errors and POST them here; we re-emit each record through
/// the standard <see cref="ILogger"/> pipeline so it flows to SigNoz via the existing
/// Serilog OTLP sink (no per-app OTLP exporter needed). Records carry a
/// <c>client.app</c> attribute so they can be filtered / counted / dashboarded per app.
///
/// Cross-cutting (not owned by any module) → lives in Orchestration per the API
/// endpoints policy. Writes nothing to the DB.
///
/// Security / PDPL:
///  - <c>AllowAnonymous</c> so login-screen errors (no JWT yet) are still captured,
///    but rate-limited (see the "client-logs" policy) to backstop a flooding device.
///  - When a Bearer token IS present, gym/user are taken from the JWT and any
///    client-supplied ids are ignored (never trust the client).
///  - Server-side scrub + truncation (defense in depth on top of client-side redaction):
///    no auth/token/password/body fields are persisted.
/// </summary>
internal sealed class ClientLogsEndpoint : IEndpoint
{
    private const int MaxRecordsPerRequest = 100;
    private const int MaxMessageLength = 4000;
    private const int MaxStackLength = 16000;
    private const int MaxAttributes = 25;
    private const int MaxAttrValueLength = 1000;

    internal sealed record ClientLogRecord(
        string? Level,
        string? Message,
        string? App,
        string? AppVersion,
        string? Os,
        string? OsVersion,
        string? Device,
        string? Route,
        string? ErrorType,
        string? StackTrace,
        string? ClientTs,
        Dictionary<string, object>? Attributes);

    internal sealed record ClientLogBatch(List<ClientLogRecord>? Records);

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

    private static string Redact(string s)
    {
        s = EmailRx.Replace(s, "[email]");
        s = TokenRx.Replace(s, "[token]");
        s = PhoneRx.Replace(s, "[phone]");
        return s;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/observability/client-logs", (
            ClientLogBatch batch,
            HttpContext http,
            ILoggerFactory loggerFactory) =>
        {
            var records = batch.Records;
            if (records is null || records.Count == 0)
                return ApiResults.Ok(new { accepted = 0 });

            // One logger keyed to a stable category so SigNoz can scope to mobile logs.
            var logger = loggerFactory.CreateLogger("GymApp.ClientTelemetry");

            // Authenticated context overrides anything the client claims.
            var userId = http.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                         ?? http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var gymId = http.User.FindFirstValue("gymId");

            var take = Math.Min(records.Count, MaxRecordsPerRequest);
            for (var i = 0; i < take; i++)
            {
                EmitRecord(logger, records[i], userId, gymId);
            }

            if (records.Count > MaxRecordsPerRequest)
            {
                logger.LogWarning(
                    "Client telemetry batch truncated: {Received} received, {Kept} kept",
                    records.Count, MaxRecordsPerRequest);
            }

            return ApiResults.Ok(new { accepted = take });
        })
        .WithTags("Observability")
        .WithSummary("Ingest client (mobile) telemetry")
        .WithDescription(
            "Accepts a batch of client log/error records from the mobile apps and forwards " +
            "them to SigNoz via the server logging pipeline. Anonymous-allowed but rate-limited; " +
            "user/gym are derived from the JWT when present.")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status429TooManyRequests)
        .AllowAnonymous()
        .RequireRateLimiting("client-logs");
    }

    private static void EmitRecord(ILogger logger, ClientLogRecord r, string? userId, string? gymId)
    {
        var level = MapLevel(r.Level);
        if (!logger.IsEnabled(level)) return;

        var scope = new Dictionary<string, object>
        {
            ["client.app"] = Sanitize(r.App) ?? "unknown",
            ["client.version"] = Sanitize(r.AppVersion) ?? "unknown",
            ["client.os"] = Sanitize(r.Os) ?? "unknown",
            ["client.os_version"] = Sanitize(r.OsVersion) ?? "unknown",
            ["client.device"] = Sanitize(r.Device) ?? "unknown",
            ["client.route"] = Sanitize(r.Route) ?? "unknown",
            ["client.ts"] = Sanitize(r.ClientTs) ?? "",
        };
        if (!string.IsNullOrEmpty(r.ErrorType))
            scope["client.error_type"] = Truncate(r.ErrorType, 200);
        if (!string.IsNullOrEmpty(r.StackTrace))
            scope["client.stack"] = Truncate(Redact(r.StackTrace), MaxStackLength);
        if (!string.IsNullOrEmpty(userId))
            scope["user.id"] = userId;
        if (!string.IsNullOrEmpty(gymId))
            scope["gym.id"] = gymId;

        // Merge scrubbed client attributes (http.method/path/status, error.code, …).
        if (r.Attributes is { Count: > 0 })
        {
            var added = 0;
            foreach (var (k, v) in r.Attributes)
            {
                if (added >= MaxAttributes) break;
                if (string.IsNullOrEmpty(k) || BlockedAttrKeys.Contains(k)) continue;
                scope[$"client.{k}"] = v switch
                {
                    string s => Truncate(Redact(s), MaxAttrValueLength),
                    null => "",
                    _ => Truncate(Redact(v.ToString() ?? ""), MaxAttrValueLength),
                };
                added++;
            }
        }

        var message = Truncate(Redact(r.Message ?? "(no message)"), MaxMessageLength);

        using (logger.BeginScope(scope))
        {
            // The constant template keeps a clean log body in SigNoz; all structured
            // context rides the scope (→ OTLP log attributes).
            logger.Log(level, "[client:{ClientApp}] {ClientMessage}",
                scope["client.app"], message);
        }
    }

    private static LogLevel MapLevel(string? level) => level?.ToLowerInvariant() switch
    {
        "fatal" => LogLevel.Critical,
        "error" => LogLevel.Error,
        "warning" or "warn" => LogLevel.Warning,
        "info" => LogLevel.Information,
        "debug" => LogLevel.Debug,
        _ => LogLevel.Information,
    };

    private static string? Sanitize(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : Truncate(s.Trim(), MaxAttrValueLength);

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max];
}
