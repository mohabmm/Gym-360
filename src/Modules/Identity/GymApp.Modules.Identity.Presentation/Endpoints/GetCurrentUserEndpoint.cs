using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Application.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/identity/me", async (HttpContext httpContext, ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            var result = await sender.Send(new GetCurrentUserQuery(userId));

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Get the authenticated user with current gym/tenant context")
        .WithDescription("Returns a fresh UserResponse (gymId, gymName, schemaPrefix-relevant fields, ownedGyms) for the JWT subject. Clients (web/mobile) call this on session hydrate to refresh stale gym context after re-seed/migration.")
        .Produces<ApiResponse<UserResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
