using System.Security.Claims;
using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.GymFeatureOverrides;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class SetGymFeatureOverrideEndpoint : IEndpoint
{
    // SetBy is optional in the body; the caller is derived from the JWT 'sub' claim when absent.
    // FeatureCode is preferred over FeatureId because each per-gym billing schema seeds its own
    // Feature ids — only the Code is stable across tenants. FeatureId still accepted for backwards
    // compat; if both are provided, FeatureId wins.
    internal sealed record SetGymFeatureOverrideRequest(
        Guid? FeatureId,
        string? FeatureCode,
        bool Enabled,
        string? Reason,
        Guid? SetBy);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/admin/gyms/{gymId:guid}/feature-overrides", async (
            Guid gymId,
            SetGymFeatureOverrideRequest request,
            HttpContext httpContext,
            ISender sender) =>
        {
            Guid setBy = request.SetBy ?? Guid.Empty;
            if (setBy == Guid.Empty)
            {
                var sub = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? httpContext.User.FindFirstValue("sub");
                if (Guid.TryParse(sub, out var fromJwt))
                    setBy = fromJwt;
            }

            var command = new SetGymFeatureOverrideCommand(
                gymId,
                request.FeatureId ?? Guid.Empty,
                request.FeatureCode,
                request.Enabled,
                request.Reason,
                setBy);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithSummary("Set gym feature override")
        .WithDescription("Creates or updates a per-gym feature override, enabling or disabling a feature regardless of the plan. Requires platform_admin role.")
        .Produces(204)
        .WithTags("Billing")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404);
    }
}
