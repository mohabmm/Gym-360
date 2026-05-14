using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.SetKioskPin;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class SetKioskPinEndpoint : IEndpoint
{
    internal sealed record Request(string NewPin, string? CurrentPin);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/gyms/{gymId:guid}/kiosk-pin", async (Guid gymId, [FromBody] Request request, ISender sender) =>
        {
            var command = new SetKioskPinCommand(gymId, request.NewPin, request.CurrentPin);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Set or change the kiosk exit PIN")
        .WithDescription("Sets a kiosk-mode exit PIN for this gym. If a PIN is already set, CurrentPin must be provided and correct.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404)
        // Owner / Manager only — `gym_owner_or_manager` policy now also accepts the
        // `staffSubRole` JWT claim populated by IdentityJwtTokenService via IStaffSubRoleResolver.
        .RequireAuthorization("gym_owner_or_manager");
    }
}
