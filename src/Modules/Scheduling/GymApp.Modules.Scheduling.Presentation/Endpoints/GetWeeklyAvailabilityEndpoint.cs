using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Application.Queries.GetWeeklyAvailability;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class GetWeeklyAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/scheduling/availability/week", async (
            Guid trainerId,
            Guid gymId,
            DateOnly startDate,
            DateOnly endDate,
            Guid? memberId,
            ISender sender) =>
        {
            var query = new GetWeeklyAvailabilityQuery(trainerId, gymId, startDate, endDate, memberId);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Get weekly availability grid")
        .WithDescription("Returns a per-day grid for the given trainer and date range. Each day is flagged off-day or blocked, otherwise carries time slots tagged Available, BookedByMe (when memberId matches), or BookedByOther.")
        .Produces<ApiResponse<WeeklyAvailabilityDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .RequireAuthorization();
    }
}
