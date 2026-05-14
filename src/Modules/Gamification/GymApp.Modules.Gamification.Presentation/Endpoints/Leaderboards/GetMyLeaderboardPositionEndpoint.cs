using GymApp.Common.Presentation;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Application.Queries.GetMyLeaderboardPosition;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gamification.Presentation.Endpoints.Leaderboards;

internal sealed class GetMyLeaderboardPositionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gamification/leaderboards/my-position", async (
            Guid userId,
            Guid gymId,
            ISender sender) =>
        {
            var query = new GetMyLeaderboardPositionQuery(userId, gymId);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get my leaderboard positions")
        .WithDescription("Returns the authenticated member's rank and score across all active leaderboards for the specified gym.")
        .WithTags("Gamification")
        .Produces<ApiResponse<MyLeaderboardPositionResponse>>(200)
        .Produces<ApiResponse<object>>(401);
    }
}
