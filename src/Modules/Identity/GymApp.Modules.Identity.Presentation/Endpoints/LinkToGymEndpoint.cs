using System.Security.Claims;
using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.LinkToGym;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class LinkToGymEndpoint : IEndpoint
{
    // UserId derived from JWT — body only carries GymCode so users can only link their own account.
    internal sealed record Request(string GymCode);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/link-gym", async (Request request, HttpContext http, ISender sender) =>
        {
            var sub = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.User.FindFirstValue("sub");
            if (!Guid.TryParse(sub, out var userId) || userId == Guid.Empty)
                return ApiResults.Problem(new Error("Auth.UserMissing", "Authenticated user id is missing."));

            var command = new LinkToGymCommand(userId, request.GymCode);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Link user account to a gym")
        .WithDescription("Associates the user with a gym using a gym invitation code. Creates a gym membership record for the user.")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
