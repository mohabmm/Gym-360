using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.Revenue;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Revenue;

internal sealed class GetRevenueByTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/billing/revenue/by-trainer", async (
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            [FromQuery(Name = "gymId")] Guid? queryGymId,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            ISender sender) =>
        {
            var resolved = headerGymId ?? queryGymId;
            if (resolved is null || resolved == Guid.Empty)
                return ApiResults.Problem(
                    "GYM_CONTEXT_REQUIRED",
                    "The X-Gym-Id header (or gymId query parameter) is required.",
                    400);

            var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var fromDate = from ?? toDate.AddMonths(-12);
            var query = new GetRevenueByTrainerQuery(resolved.Value, fromDate, toDate);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get revenue breakdown by trainer")
        .WithDescription("Returns per-trainer revenue and session counts for a gym over a specified date range.")
        .WithTags("Billing")
        .Produces<ApiResponse<List<TrainerRevenueDto>>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }
}
