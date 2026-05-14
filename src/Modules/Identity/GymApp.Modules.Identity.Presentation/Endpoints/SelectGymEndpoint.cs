using System.Security.Claims;
using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.SelectGym;
using GymApp.Modules.Identity.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class SelectGymEndpoint : IEndpoint
{
    internal sealed record Request(Guid GymId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/select-gym", async (
            Request request,
            HttpContext httpContext,
            ISender sender) =>
        {
            var userIdClaim = httpContext.User.FindFirstValue("sub")
                ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
                return ApiResults.Problem(new Common.Domain.Error("SelectGym.Unauthenticated", "Missing user identity."));

            var command = new SelectGymCommand(userId, request.GymId);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Owner: select active gym")
        .WithDescription("Re-issues a JWT scoped to the chosen gym. Owner-only — returns 403 for any other principal type.")
        .Produces<ApiResponse<TokenResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
        .RequireAuthorization();
    }
}
