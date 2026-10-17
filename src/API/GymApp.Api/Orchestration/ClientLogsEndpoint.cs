using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static GymApp.Api.Orchestration.ClientLogScrubber;
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
    private const int MaxAttributes = 25;

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
            var identity = new ClientIdentity(
                UserId: http.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? http.User.FindFirstValue(ClaimTypes.NameIdentifier),
                GymId: http.User.FindFirstValue("gymId"));

            var take = Math.Min(records.Count, MaxRecordsPerRequest);
            for (var i = 0; i < take; i++)
            {
                EmitRecord(logger, records[i], identity);
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

    /// <summary>Authenticated user/gym taken from the JWT (null when anonymous).</summary>
    private sealed record ClientIdentity(string? UserId, string? GymId);

    private static void EmitRecord(ILogger logger, ClientLogRecord r, ClientIdentity identity)
    {
        var level = MapLevel(r.Level);
        if (!logger.IsEnabled(level)) return;

        var scope = BuildScope(r, identity);
        var message = Truncate(Redact(r.Message ?? "(no message)"), MaxMessageLength);

        using (logger.BeginScope(scope))
        {
            // The constant template keeps a clean log body in SigNoz; all structured
            // context rides the scope (→ OTLP log attributes).
            logger.Log(level, "[client:{ClientApp}] {ClientMessage}",
                scope["client.app"], message);
        }
    }

    // Optional record fields, forwarded (scrubbed) only when the client sent a value.
    private static readonly (string Key, Func<ClientLogRecord, string?> Read, Func<string, string> Scrub)[] OptionalRecordFields =
    {
        ("client.error_type", r => r.ErrorType, v => Truncate(v, MaxErrorTypeLength)),
        ("client.stack", r => r.StackTrace, v => Truncate(Redact(v), MaxStackLength)),
    };

    private static Dictionary<string, object> BuildScope(ClientLogRecord r, ClientIdentity identity)
    {
        var scope = new Dictionary<string, object>
        {
            ["client.app"] = OrUnknown(r.App),
            ["client.version"] = OrUnknown(r.AppVersion),
            ["client.os"] = OrUnknown(r.Os),
            ["client.os_version"] = OrUnknown(r.OsVersion),
            ["client.device"] = OrUnknown(r.Device),
            ["client.route"] = OrUnknown(r.Route),
            ["client.ts"] = Sanitize(r.ClientTs) ?? "",
        };

        foreach (var (key, read, scrub) in OptionalRecordFields)
        {
            var value = read(r);
            if (!string.IsNullOrEmpty(value))
                scope[key] = scrub(value);
        }

        AddIdentity(scope, identity);

        // Merge scrubbed client attributes (http.method/path/status, error.code, …).
        if (r.Attributes is { Count: > 0 })
            MergeClientAttributes(scope, r.Attributes);

        return scope;
    }

    private static void AddIdentity(Dictionary<string, object> scope, ClientIdentity identity)
    {
        if (!string.IsNullOrEmpty(identity.UserId))
            scope["user.id"] = identity.UserId;
        if (!string.IsNullOrEmpty(identity.GymId))
            scope["gym.id"] = identity.GymId;
    }

    private static void MergeClientAttributes(
        Dictionary<string, object> scope,
        Dictionary<string, object> attributes)
    {
        var added = 0;
        foreach (var (k, v) in attributes)
        {
            if (added >= MaxAttributes) break;
            if (IsBlockedAttribute(k)) continue;
            scope[$"client.{k}"] = ScrubAttributeValue(v);
            added++;
        }
    }

    private static readonly Dictionary<string, LogLevel> LevelsByName = new()
    {
        ["fatal"] = LogLevel.Critical,
        ["error"] = LogLevel.Error,
        ["warning"] = LogLevel.Warning,
        ["warn"] = LogLevel.Warning,
        ["info"] = LogLevel.Information,
        ["debug"] = LogLevel.Debug,
    };

    private static LogLevel MapLevel(string? level) =>
        LevelsByName.GetValueOrDefault(level?.ToLowerInvariant() ?? string.Empty, LogLevel.Information);

    private static string OrUnknown(string? value) => Sanitize(value) ?? "unknown";
}
