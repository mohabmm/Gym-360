using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Application.Queries.GetBodyMetrics;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Progress.Presentation.Endpoints;

internal sealed class GetBodyMetricsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/progress/body-metrics", async (
            Guid gymId,
            DateOnly? from,
            DateOnly? to,
            HttpContext http,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(http, out var userId))
                return ApiResults.Problem(new Error("Auth.UserMissing", "Authenticated user id is missing."));

            var query = new GetBodyMetricsQuery(userId, gymId, from, to);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Progress")
        .WithSummary("Get body metrics history")
        .WithDescription("Returns a list of body metric entries (weight, body fat, measurements) for a member within an optional date range.")
        .Produces<ApiResponse<List<BodyMetricDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
