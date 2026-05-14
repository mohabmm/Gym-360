using GymApp.Common.Presentation;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Application.Queries.GetCoachOfTheMonth;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gamification.Presentation.Endpoints.Trainers;

internal sealed class GetCoachOfTheMonthEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gamification/trainers/coach-of-the-month", async (
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            [FromQuery(Name = "gymId")] Guid? queryGymId,
            int month,
            int year,
            ISender sender) =>
        {
            var resolved = headerGymId ?? queryGymId;
            if (resolved is null || resolved == Guid.Empty)
                return ApiResults.Problem(
                    "GYM_CONTEXT_REQUIRED",
                    "The X-Gym-Id header (or gymId query parameter) is required.",
                    400);

            var query = new GetCoachOfTheMonthQuery(resolved.Value, month, year);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get coach of the month")
        .WithDescription("Returns the top-scoring trainer for the specified gym, month, and year, or indicates no coach if scores are unavailable.")
        .WithTags("Gamification")
        .Produces<ApiResponse<CoachOfTheMonthResponse>>(200)
        .Produces<ApiResponse<object>>(401);
    }
}
