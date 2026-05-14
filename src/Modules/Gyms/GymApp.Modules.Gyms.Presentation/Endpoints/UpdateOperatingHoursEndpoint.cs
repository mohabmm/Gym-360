using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.UpdateOperatingHours;
using GymApp.Modules.Gyms.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class UpdateOperatingHoursEndpoint : IEndpoint
{
    internal sealed record Request(List<DayScheduleDto> Hours);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/gyms/{gymId:guid}/operating-hours", async (Guid gymId, Request request, ISender sender) =>
        {
            var command = new UpdateOperatingHoursCommand(gymId, request.Hours ?? new());
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Update gym operating hours")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
