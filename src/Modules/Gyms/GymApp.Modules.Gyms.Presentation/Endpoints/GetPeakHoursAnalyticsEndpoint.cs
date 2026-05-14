using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Application.Queries.GetPeakHoursAnalytics;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class GetPeakHoursAnalyticsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}/analytics/peak-hours", async (
            Guid gymId,
            DateTime? from,
            DateTime? to,
            int? tzOffsetMinutes,
            ISender sender) =>
        {
            // Default offset: UTC+3 (Africa/Cairo summer). Clients should pass
            // the user's actual offset so bucketing aligns with local hours.
            var query = new GetPeakHoursAnalyticsQuery(
                gymId, from, to, tzOffsetMinutes ?? 180);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Get peak-hours attendance heatmap (Pro)")
        .WithDescription("Returns check-in counts bucketed by local day-of-week (0=Sunday..6=Saturday) and hour (0..23) over the requested range. Requires the peak_hours_analytics feature.")
        .Produces<ApiResponse<PeakHoursAnalyticsResponse>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
