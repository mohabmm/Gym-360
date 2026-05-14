using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Application.Queries.GetKioskPinInfo;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class GetKioskPinInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}/kiosk-pin", async (Guid gymId, ISender sender) =>
        {
            var result = await sender.Send(new GetKioskPinInfoQuery(gymId));

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<KioskPinInfoResponse>(result.Value))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Get kiosk PIN state (and hash for offline verify)")
        .WithDescription("Returns { isSet, hash }. The hash is the BCrypt hash of the current PIN — cache it client-side to allow kiosk exit while offline.")
        .Produces<ApiResponse<KioskPinInfoResponse>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization("gym_staff");
    }
}
