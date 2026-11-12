using System.Security.Claims;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using Microsoft.AspNetCore.Mvc;

namespace GymApp.Api.FeatureFlags;

/// <summary>
/// <c>GET /api/v1/feature-flags/me</c> — resolved flags for the caller's gym. All apps call this at
/// start, after login, on resume and every few minutes, and cache the last response.
/// </summary>
internal sealed class MyFeatureFlagsEndpoint : IEndpoint
{
    /// <summary>How often clients should refetch while open (matches the server cache TTL).</summary>
    internal const int RefreshAfterSeconds = 300;

    internal sealed record MyFeatureFlagsResponse(
        Guid GymId,
        string Preset,
        IReadOnlyDictionary<string, bool> Flags,
        IReadOnlyList<string> Enabled,
        int RefreshAfterSeconds,
        DateTime ResolvedAtUtc);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/feature-flags/me", async (
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            HttpContext httpContext,
            ITenantContext tenant,
            IFeatureFlagService flags,
            CancellationToken cancellationToken) =>
        {
            var gymId = ResolveGymId(httpContext.User, tenant, headerGymId);
            if (gymId is null)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "No gym is selected for this user.", StatusCodes.Status400BadRequest);

            var resolved = await flags.GetResolvedAsync(gymId.Value, cancellationToken);
            return ApiResults.Ok(new MyFeatureFlagsResponse(
                resolved.GymId,
                resolved.PresetCode,
                resolved.Flags.ToDictionary(f => f.Key, f => f.Enabled),
                resolved.EnabledKeys(),
                RefreshAfterSeconds,
                resolved.ResolvedAtUtc));
        })
        .RequireAuthorization()
        .AlwaysOn()
        .WithTags("FeatureFlags")
        .WithSummary("Get resolved feature flags for the current gym")
        .WithDescription("Returns every flag key with its effective value (override > preset > OFF) for the caller's gym. The gym comes from the JWT (selected gym) or the X-Gym-Id header.")
        .Produces<ApiResponse<MyFeatureFlagsResponse>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }

    // JWT/tenant gym wins so a member cannot read another gym's flags by changing the header,
    // except for principals without a gym claim (owners), who address gyms by header.
    private static Guid? ResolveGymId(ClaimsPrincipal user, ITenantContext tenant, Guid? headerGymId)
    {
        if (Guid.TryParse(user.FindFirstValue("gymId"), out var fromClaim))
            return fromClaim;
        if (tenant.GymId is { } fromTenant && fromTenant != Guid.Empty)
            return fromTenant;
        return headerGymId is { } h && h != Guid.Empty ? h : null;
    }
}
