using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Application.Queries.GetSessionStats;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class GetSessionStatsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/scheduling/sessions/stats", async (
            Guid trainerId,
            Guid gymId,
            DateOnly? from,
            DateOnly? to,
            ISender sender) =>
        {
            var query = new GetSessionStatsQuery(trainerId, gymId, from, to);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Get session statistics")
        .WithDescription("Returns aggregated session statistics for a trainer including completed, cancelled, and no-show counts and the overall completion rate within an optional date range.")
        .Produces<ApiResponse<SessionStatsResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
