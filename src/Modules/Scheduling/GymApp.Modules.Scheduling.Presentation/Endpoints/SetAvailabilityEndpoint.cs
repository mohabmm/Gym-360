using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.Commands.SetAvailability;
using GymApp.Modules.Scheduling.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class SetAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/scheduling/availability", async (SetAvailabilityRequest request, ISender sender) =>
        {
            var command = new SetAvailabilityCommand(
                request.TrainerId,
                request.GymId,
                request.SessionDurationMinutes,
                request.BufferMinutes,
                request.WeeklySlots);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Set trainer availability")
        .WithDescription("Creates or replaces a trainer's weekly availability schedule including session duration, buffer time, and day/time slots for a given gym.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}

internal sealed record SetAvailabilityRequest(
    Guid TrainerId,
    Guid GymId,
    int SessionDurationMinutes,
    int BufferMinutes,
    List<AvailabilitySlotDto> WeeklySlots);
