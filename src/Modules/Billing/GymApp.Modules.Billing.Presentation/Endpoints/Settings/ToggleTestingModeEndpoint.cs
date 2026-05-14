using System.Security.Claims;
using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.GymFeatureOverrides;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Settings;

// Owner/manager-controllable wrapper around the early_session_start feature override.
// The trainer mobile app reads this flag to permit pre-window session starts during
// fixture testing. Underlying storage is the same GymFeatureOverride row that
// StartSessionCommandHandler already consults via IGymPlanService.
internal sealed class ToggleTestingModeEndpoint : IEndpoint
{
    internal sealed record Request(bool Enabled);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/settings/testing-mode", async (
            Guid gymId,
            Request request,
            HttpContext httpContext,
            ISender sender) =>
        {
            var sub = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? httpContext.User.FindFirstValue("sub");
            Guid.TryParse(sub, out var setBy);

            var command = new SetGymFeatureOverrideCommand(
                gymId,
                Guid.Empty,
                "early_session_start",
                request.Enabled,
                "testing-mode toggle",
                setBy);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("gym_owner_or_manager")
        .WithTags("Gyms")
        .WithSummary("Toggle gym testing mode (early session start)")
        .WithDescription("Owner/manager toggle that flips the `early_session_start` feature override for this gym. When enabled, trainers can start sessions before the 15-min pre-window guard — used during fixture testing. Underlying storage is a GymFeatureOverride row, so the trainer's StartSession check picks it up without any further wiring.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);
    }
}
