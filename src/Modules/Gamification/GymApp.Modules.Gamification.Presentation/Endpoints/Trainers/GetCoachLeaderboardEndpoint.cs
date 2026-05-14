using GymApp.Common.Presentation;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Application.Queries.GetCoachLeaderboard;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gamification.Presentation.Endpoints.Trainers;

internal sealed class GetCoachLeaderboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gamification/trainers/leaderboard", async (
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            [FromQuery(Name = "gymId")] Guid? queryGymId,
            ISender sender) =>
        {
            var resolved = headerGymId ?? queryGymId;
            if (resolved is null || resolved == Guid.Empty)
                return ApiResults.Problem(
                    "GYM_CONTEXT_REQUIRED",
                    "The X-Gym-Id header (or gymId query parameter) is required.",
                    400);

            var query = new GetCoachLeaderboardQuery(resolved.Value);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get coach leaderboard for a gym")
        .WithDescription("Returns all trainers ranked by normalized score for the specified gym's current period.")
        .WithTags("Gamification")
        .Produces<ApiResponse<List<CoachLeaderboardEntryDto>>>(200)
        .Produces<ApiResponse<object>>(401);
    }
}
