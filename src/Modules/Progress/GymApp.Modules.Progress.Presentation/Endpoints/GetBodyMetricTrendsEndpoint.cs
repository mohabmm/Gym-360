using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Application.Queries.GetBodyMetricTrends;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Progress.Presentation.Endpoints;

internal sealed class GetBodyMetricTrendsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/progress/body-metrics/trends", async (
            Guid gymId,
            DateOnly from,
            DateOnly to,
            HttpContext http,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(http, out var userId))
                return ApiResults.Problem(new Error("Auth.UserMissing", "Authenticated user id is missing."));

            var query = new GetBodyMetricTrendsQuery(userId, gymId, from, to);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Progress")
        .WithSummary("Get body metric trends")
        .WithDescription("Returns time-series trend data for weight, body fat, waist, and chest measurements over a specified date range.")
        .Produces<ApiResponse<MetricTrendsResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
