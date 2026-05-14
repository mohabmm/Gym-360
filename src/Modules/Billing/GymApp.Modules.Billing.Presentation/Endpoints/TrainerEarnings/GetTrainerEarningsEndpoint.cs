using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Application.Queries.GetTrainerEarnings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.TrainerEarnings;

internal sealed class GetTrainerEarningsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/billing/trainers/{trainerId:guid}/earnings",
            async (
                Guid trainerId,
                [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
                [FromQuery(Name = "gymId")] Guid? queryGymId,
                DateOnly? from,
                DateOnly? to,
                ISender sender) =>
            {
                var resolved = headerGymId ?? queryGymId;
                if (resolved is null || resolved == Guid.Empty)
                    return ApiResults.Problem(
                        "GYM_CONTEXT_REQUIRED",
                        "The X-Gym-Id header (or gymId query parameter) is required.",
                        400);

                var query = new GetTrainerEarningsQuery(resolved.Value, trainerId, from, to);
                var result = await sender.Send(query);

                return result.IsSuccess
                    ? ApiResults.Ok(result.Value)
                    : ApiResults.Problem(result.Error);
            })
        .WithTags("Billing")
        .WithSummary("Get trainer earnings summary")
        .WithDescription("Returns the earnings summary for a trainer at a specific gym within the given date range. Trainers can only see their own earnings; gym_staff and gym_owner can see any trainer's earnings.")
        .Produces<ApiResponse<TrainerEarningsSummaryDto>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();
    }
}
