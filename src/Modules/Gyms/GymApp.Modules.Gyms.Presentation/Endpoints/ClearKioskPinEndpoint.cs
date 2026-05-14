using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.ClearKioskPin;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class ClearKioskPinEndpoint : IEndpoint
{
    internal sealed record Request(string CurrentPin);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/gyms/{gymId:guid}/kiosk-pin", async (Guid gymId, [FromBody] Request request, ISender sender) =>
        {
            var command = new ClearKioskPinCommand(gymId, request.CurrentPin);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Remove the kiosk exit PIN")
        .WithDescription("Removes the kiosk exit PIN. Kiosk mode will exit without prompting. Requires the current PIN to confirm.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404)
        // Owner / Manager only — `gym_owner_or_manager` policy now reads `staffSubRole` claim.
        .RequireAuthorization("gym_owner_or_manager");
    }
}
