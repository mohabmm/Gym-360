using System.Security.Claims;
using System.Text.Json;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Presentation.FeatureFlags;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Api.Middleware;

/// <summary>
/// Blocks API calls to features that are OFF for the caller's gym with
/// <c>403 { errors: [{ code: "FEATURE_DISABLED:&lt;key&gt;" }] }</c>.
/// The owning feature comes from endpoint metadata, else <see cref="FeatureRouteMap"/>.
/// Runs after tenant resolution; platform admins (no gym) are never gated.
/// </summary>
public sealed class FeatureFlagEnforcementMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestDelegate _next;
    private readonly ILogger<FeatureFlagEnforcementMiddleware> _logger;

    public FeatureFlagEnforcementMiddleware(RequestDelegate next, ILogger<FeatureFlagEnforcementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenant, IFeatureFlagService flags)
    {
        var featureKeys = ResolveFeatureKeys(context);
        if (featureKeys is null || IsPlatformAdmin(context.User))
        {
            await _next(context);
            return;
        }

        var gymId = ResolveGymId(context, tenant);
        if (gymId is null)
        {
            // No gym in scope: the handler's own gym checks apply (nothing to gate against).
            await _next(context);
            return;
        }

        if (await AnyEnabledAsync(flags, gymId.Value, featureKeys, context.RequestAborted))
        {
            await _next(context);
            return;
        }

        _logger.LogInformation("Blocked {Method} {Path}: feature {Feature} is off for gym {GymId}",
            context.Request.Method, context.Request.Path, string.Join(" | ", featureKeys), gymId);
        await WriteDisabledAsync(context, featureKeys[0]);
    }

    private static async Task<bool> AnyEnabledAsync(
        IFeatureFlagService flags, Guid gymId, IReadOnlyList<string> featureKeys, CancellationToken cancellationToken)
    {
        foreach (var key in featureKeys)
        {
            if (await flags.IsEnabledAsync(gymId, key, cancellationToken))
                return true;
        }

        return false;
    }

    /// <summary>Feature keys gating the matched endpoint (any one ON opens it), or null when always on / unmatched.</summary>
    internal static IReadOnlyList<string>? ResolveFeatureKeys(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
            return null;

        if (endpoint.Metadata.GetMetadata<RequireFeatureMetadata>() is { } required)
            return new[] { required.FeatureKey };
        if (endpoint.Metadata.GetMetadata<RequireAnyFeatureMetadata>() is { FeatureKeys.Count: > 0 } any)
            return any.FeatureKeys;
        if (endpoint.Metadata.GetMetadata<AlwaysOnFeatureMetadata>() is not null)
            return null;

        return MappedFeatureKeys(context, endpoint);
    }

    private static IReadOnlyList<string>? MappedFeatureKeys(HttpContext context, Endpoint endpoint)
    {
        if (endpoint is not RouteEndpoint route || route.RoutePattern.RawText is not { } template)
            return null;

        var key = FeatureRouteMap.Resolve(new RouteRequest(context.Request.Method, template))?.FeatureKey;
        return key is null ? null : new[] { key };
    }

    private static bool IsPlatformAdmin(ClaimsPrincipal user) =>
        user.HasClaim(c => c.Type == "adminRole")
        || string.Equals(user.FindFirstValue("principalType"), "PlatformAdmin", StringComparison.OrdinalIgnoreCase);

    private static Guid? ResolveGymId(HttpContext context, ITenantContext tenant)
    {
        if (tenant.GymId is { } resolved && resolved != Guid.Empty)
            return resolved;

        if (Guid.TryParse(context.GetRouteValue("gymId")?.ToString(), out var fromRoute))
            return fromRoute;
        if (Guid.TryParse(context.Request.Headers["X-Gym-Id"].FirstOrDefault(), out var fromHeader))
            return fromHeader;
        if (Guid.TryParse(context.User.FindFirstValue("gymId"), out var fromClaim))
            return fromClaim;

        return null;
    }

    private static Task WriteDisabledAsync(HttpContext context, string featureKey)
    {
        var error = FeatureFlagErrors.FeatureDisabled(featureKey);
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            data = (object?)null,
            errors = new[] { new { code = error.Code, message = error.Name, meta = error.Meta } },
            meta = (object?)null,
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
