using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.Analytics;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Admin;

internal sealed class GetPlatformAnalyticsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/admin/analytics", async (ISender sender) =>
        {
            var result = await sender.Send(new GetPlatformAnalyticsQuery());
            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithTags("Admin", "Analytics")
        .WithSummary("Platform analytics summary")
        .WithDescription("KPIs, plan distribution, gym growth, revenue trend, at-risk gyms. Requires platform_admin.")
        .Produces<ApiResponse<PlatformAnalyticsDto>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403);
    }
}

internal sealed class GetChurnAnalyticsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/admin/analytics/churn", async (ISender sender) =>
        {
            var result = await sender.Send(new GetChurnAnalyticsQuery());
            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("platform_admin")
        .WithTags("Admin", "Analytics")
        .WithSummary("Churn analytics")
        .WithDescription("Churn rate, by plan, over time, at-risk gyms, and cohort retention. Requires platform_admin.")
        .Produces<ApiResponse<ChurnAnalyticsDto>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403);
    }
}
