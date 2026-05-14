using GymApp.Common.Presentation;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Application.Queries.GetProgressDashboard;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Progress.Presentation.Endpoints;

internal sealed class GetProgressDashboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/progress/dashboard", async (
            Guid userId,
            Guid gymId,
            ISender sender) =>
        {
            var query = new GetProgressDashboardQuery(userId, gymId);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Progress")
        .WithSummary("Get progress dashboard")
        .WithDescription("Returns the authenticated member's progress dashboard including latest body metrics, weight/body fat change, photo count, and current and longest workout and check-in streaks.")
        .Produces<ApiResponse<ProgressDashboardResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}
