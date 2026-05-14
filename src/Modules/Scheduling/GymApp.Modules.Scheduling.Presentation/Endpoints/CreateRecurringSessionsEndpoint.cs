using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.Commands.CreateRecurringSessions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class CreateRecurringSessionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/scheduling/sessions/recurring", async (CreateRecurringSessionsRequest request, ISender sender) =>
        {
            var command = new CreateRecurringSessionsCommand(
                request.GymId,
                request.TrainerId,
                request.MemberId,
                request.DayOfWeek,
                request.Time,
                request.DurationMinutes,
                request.SessionType,
                request.WeeksAhead);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Create recurring sessions")
        .WithDescription("Schedules a repeating weekly session for a trainer-member pair over a specified number of weeks. Returns a list of created session IDs.")
        .Produces<ApiResponse<List<Guid>>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

internal sealed record CreateRecurringSessionsRequest(
    Guid GymId,
    Guid TrainerId,
    Guid MemberId,
    DayOfWeek DayOfWeek,
    TimeOnly Time,
    int DurationMinutes,
    string SessionType,
    int WeeksAhead);
