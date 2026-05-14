using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Application.Queries.GetUpcomingSessions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class GetUpcomingSessionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/scheduling/sessions/upcoming", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymIdHeader,
            [FromQuery] Guid? gymId,
            [FromQuery] Guid? userId,
            [FromQuery] string? role,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var authenticatedUserId))
                return Results.Unauthorized();

            var resolvedGymId = gymId ?? gymIdHeader ?? UserContext.TryGetGymId(httpContext);
            if (resolvedGymId is null || resolvedGymId == Guid.Empty)
                return ApiResults.Problem(
                    "GYM_CONTEXT_REQUIRED",
                    "The X-Gym-Id header (or gymId query parameter) is required.",
                    400);

            var resolvedUserId = userId ?? authenticatedUserId;
            var resolvedRole = role ?? UserContext.GetRole(httpContext) ?? "trainee";

            var query = new GetUpcomingSessionsQuery(resolvedUserId, resolvedGymId.Value, resolvedRole);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Get upcoming sessions")
        .WithDescription("Returns the next upcoming sessions for the authenticated user at the gym specified by X-Gym-Id (or gymId query parameter). Role is derived from the JWT; userId/role query parameters may override for admin tooling.")
        .Produces<ApiResponse<List<SessionDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}
