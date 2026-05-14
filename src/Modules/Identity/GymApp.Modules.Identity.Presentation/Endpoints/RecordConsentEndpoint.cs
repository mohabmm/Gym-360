using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.RecordConsent;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class RecordConsentEndpoint : IEndpoint
{
    // NOTE: UserId is NOT accepted from the client — it is read from the JWT.
    // IpAddress is taken from the server-side connection, not client-supplied.
    internal sealed record Request(
        string ConsentType,
        bool IsGranted,
        string ConsentText);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/consent", async (Request request, HttpContext httpContext, ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            var ipAddress = UserContext.GetClientIp(httpContext) ?? "unknown";

            var command = new RecordConsentCommand(
                userId,
                request.ConsentType,
                request.IsGranted,
                request.ConsentText,
                ipAddress);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity", "PDPL")
        .WithSummary("Record user consent (PDPL)")
        .WithDescription("Records the authenticated user's consent or withdrawal for a specific data processing purpose, as required by Egypt's PDPL. User id is derived from the JWT; IP address is captured server-side for audit.")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}
