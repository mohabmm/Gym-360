using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.UpdateSeasonalHours;
using GymApp.Modules.Gyms.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class UpdateSeasonalHoursEndpoint : IEndpoint
{
    internal sealed record OverrideRequest(
        string Name,
        string StartDate,
        string EndDate,
        bool IsActive,
        List<DayScheduleDto>? DaySchedules);

    internal sealed record Request(List<OverrideRequest> Overrides);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/gyms/{gymId:guid}/seasonal-hours", async (Guid gymId, Request request, ISender sender) =>
        {
            var overrides = (request.Overrides ?? new())
                .Select(o => new SeasonalOverrideInput(o.Name, o.StartDate, o.EndDate, o.IsActive, o.DaySchedules))
                .ToList();
            var command = new UpdateSeasonalHoursCommand(gymId, overrides);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Update gym seasonal hour overrides (Ramadan, holidays)")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
