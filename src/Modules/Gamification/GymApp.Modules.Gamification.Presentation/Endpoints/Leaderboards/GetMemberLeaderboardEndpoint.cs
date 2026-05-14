using GymApp.Common.Presentation;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Application.Queries.GetMemberLeaderboard;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gamification.Presentation.Endpoints.Leaderboards;

internal sealed class GetMemberLeaderboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gamification/leaderboards/{type}", async (
            string type,
            Guid gymId,
            int? month,
            int? year,
            HttpContext httpContext,
            ISender sender) =>
        {
            Guid? requestingUserId = UserContext.TryGetUserId(httpContext, out var userId) ? userId : null;

            var query = new GetMemberLeaderboardQuery(gymId, type, month, year, requestingUserId);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get member leaderboard by type")
        .WithDescription("Returns the ranked member leaderboard for a given gym, leaderboard type, and optional month/year filter. Valid `type` values: `Attendance`, `WeightLoss`, `Consistency`.")
        .WithTags("Gamification")
        .Produces<ApiResponse<LeaderboardResponse>>(200)
        .Produces<ApiResponse<object>>(401);
    }
}
