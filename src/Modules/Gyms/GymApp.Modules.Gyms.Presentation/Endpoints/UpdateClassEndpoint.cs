using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.UpdateClass;
using GymApp.Modules.Gyms.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class UpdateClassEndpoint : IEndpoint
{
    internal sealed record Request(
        string Name,
        string? Description,
        ClassScheduleDto Schedule,
        int Capacity,
        bool WaitlistEnabled = true);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/gyms/{gymId:guid}/classes/{classId:guid}", async (
            Guid gymId,
            Guid classId,
            Request request,
            ISender sender) =>
        {
            var command = new UpdateClassCommand(
                gymId,
                classId,
                request.Name,
                request.Description,
                request.Schedule,
                request.Capacity,
                request.WaitlistEnabled);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Update a gym class")
        .WithDescription("Updates an existing class's name, description, schedule, capacity, and waitlist setting.")
        .Produces(204)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
