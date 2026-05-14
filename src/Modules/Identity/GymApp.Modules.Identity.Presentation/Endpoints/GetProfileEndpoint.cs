using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Application.Queries.GetProfile;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class GetProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/identity/profile", async (HttpContext httpContext, ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            var query = new GetProfileQuery(userId);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Identity")
        .WithSummary("Get the authenticated user's profile")
        .WithDescription("Returns the full profile of the authenticated user including gym memberships and account status. User id is derived from the JWT; clients cannot request another user's profile.")
        .Produces<ApiResponse<UserProfileResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
