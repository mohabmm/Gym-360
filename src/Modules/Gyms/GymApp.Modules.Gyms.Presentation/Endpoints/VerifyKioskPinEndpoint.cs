using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Queries.VerifyKioskPin;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class VerifyKioskPinEndpoint : IEndpoint
{
    internal sealed record Request(string Pin);
    internal sealed record Response(bool Verified);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/kiosk-pin/verify", async (Guid gymId, [FromBody] Request request, ISender sender) =>
        {
            var query = new VerifyKioskPinQuery(gymId, request.Pin);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<Response>(new Response(result.Value)))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Verify a kiosk exit PIN attempt")
        .WithDescription("Returns { verified: true } if the PIN matches. When no PIN is configured, always returns verified: true (no lock).")
        .Produces<ApiResponse<Response>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization("gym_staff");
    }
}
