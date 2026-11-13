using System.Security.Claims;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using Microsoft.AspNetCore.Mvc;

namespace GymApp.Api.FeatureFlags;

/// <summary>
/// Super Admin feature-flag API under <c>/api/v1/admin/feature-flags</c>. Policy <c>super_admin</c>:
/// every other role (admin, support, analyst, gym staff, trainers, trainees) gets 403.
/// </summary>
internal sealed class AdminFeatureFlagEndpoints : IEndpoint
{
    internal const string Policy = "super_admin";
    private const string Base = "api/v1/admin/feature-flags";

    internal sealed record SetPresetRequest(string PresetCode);

    internal sealed record BulkSetPresetRequest(IReadOnlyList<Guid> GymIds, string PresetCode);

    internal sealed record SetOverrideRequest(bool Enabled, string? Reason);

    internal sealed record UpdatePresetRequest(IReadOnlyDictionary<string, bool> Values);

    internal sealed record ChangeResult(int AffectedGyms);

    /// <summary>Query string of <c>GET gyms</c>.</summary>
    internal sealed record GymListFilter(
        [FromQuery] string? Search, [FromQuery] string? Preset, [FromQuery] int? Page, [FromQuery] int? PageSize);

    /// <summary>Query string of <c>GET audit</c>.</summary>
    internal sealed record AuditFilter(
        [FromQuery] Guid? GymId, [FromQuery] string? FeatureKey, [FromQuery] int? Page, [FromQuery] int? PageSize);

    /// <summary>Route values of the per-gym override endpoints.</summary>
    internal sealed record OverrideRoute(Guid GymId, string FeatureKey);

    /// <summary>The calling Super Admin and the admin service, shared by every write endpoint.</summary>
    internal sealed record AdminCall(HttpContext Http, IFeatureFlagAdminService Service);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Base)
            .RequireAuthorization(Policy)
            .AlwaysOn()
            .WithTags("FeatureFlags (Super Admin)");

        group.MapGet("catalog", (IFeatureFlagAdminService svc) => ApiResults.Ok(svc.GetCatalog()))
            .WithSummary("List every feature flag defined in code");

        group.MapGet("gyms", async ([AsParameters] GymListFilter filter, IFeatureFlagAdminService svc, CancellationToken ct) =>
                ApiResults.Ok(await svc.ListGymsAsync(
                    new GymListQuery(filter.Search, filter.Preset, filter.Page ?? 1, filter.PageSize ?? 50), ct)))
            .WithSummary("List gyms with their preset and override count");

        group.MapGet("gyms/{gymId:guid}", async (Guid gymId, IFeatureFlagAdminService svc, CancellationToken ct) =>
                ToResult(await svc.GetGymAsync(gymId, ct)))
            .WithSummary("Get a gym's flags: effective value and source (preset or override)");

        group.MapPut("gyms/{gymId:guid}/preset", async (Guid gymId, SetPresetRequest request, [AsParameters] AdminCall call) =>
                ToChangeResult(await call.Service.SetGymPresetAsync(
                    new[] { gymId }, request.PresetCode, Actor(call.Http), call.Http.RequestAborted)))
            .WithSummary("Set a gym's preset");

        group.MapPut("gyms/preset", async (BulkSetPresetRequest request, [AsParameters] AdminCall call) =>
                request.GymIds is { Count: > 0 }
                    ? ToChangeResult(await call.Service.SetGymPresetAsync(
                        request.GymIds, request.PresetCode, Actor(call.Http), call.Http.RequestAborted))
                    : ApiResults.Problem("VALIDATION_ERROR", "Select at least one gym.", 400))
            .WithSummary("Apply a preset to several gyms (bulk)");

        group.MapPut("gyms/{gymId:guid}/overrides/{featureKey}", async (
                [AsParameters] OverrideRoute route, SetOverrideRequest request, [AsParameters] AdminCall call) =>
                ToNoContent(await call.Service.SetOverrideAsync(
                    route.GymId,
                    new FlagOverrideChange(route.FeatureKey, request.Enabled, request.Reason),
                    Actor(call.Http),
                    call.Http.RequestAborted)))
            .WithSummary("Set a per-gym override");

        group.MapDelete("gyms/{gymId:guid}/overrides/{featureKey}", async ([AsParameters] OverrideRoute route, [AsParameters] AdminCall call) =>
                ToNoContent(await call.Service.ClearOverrideAsync(
                    route.GymId, route.FeatureKey, Actor(call.Http), call.Http.RequestAborted)))
            .WithSummary("Clear an override (reset to preset)");

        group.MapGet("presets", async (IFeatureFlagAdminService svc, CancellationToken ct) =>
                ApiResults.Ok(await svc.ListPresetsAsync(ct)))
            .WithSummary("List presets with their flag values and gym counts");

        group.MapPut("presets/{presetCode}", async (string presetCode, UpdatePresetRequest request, [AsParameters] AdminCall call) =>
                ToChangeResult(await call.Service.UpdatePresetAsync(
                    presetCode, request.Values, Actor(call.Http), call.Http.RequestAborted)))
            .WithSummary("Edit which flags a preset turns on");

        group.MapGet("audit", async ([AsParameters] AuditFilter filter, IFeatureFlagAdminService svc, CancellationToken ct) =>
                ApiResults.Ok(await svc.GetAuditAsync(
                    new AuditQuery(filter.GymId, filter.FeatureKey, filter.Page ?? 1, filter.PageSize ?? 50), ct)))
            .WithSummary("Read the feature-flag audit log");
    }

    private static AdminActor Actor(HttpContext http)
    {
        CallerContext.TryGetUserId(http, out var userId);
        var email = http.User.FindFirstValue(ClaimTypes.Email) ?? http.User.FindFirstValue("email");
        return new AdminActor(userId, email);
    }

    private static IResult ToResult<T>(Result<T> result) =>
        result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Problem(result.Error);

    private static IResult ToChangeResult(Result<int> result) =>
        result.IsSuccess ? ApiResults.Ok(new ChangeResult(result.Value)) : ApiResults.Problem(result.Error);

    private static IResult ToNoContent(Result result) =>
        result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result.Error);
}
