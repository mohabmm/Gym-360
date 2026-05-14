using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.CreateClass;
using GymApp.Modules.Gyms.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class CreateClassEndpoint : IEndpoint
{
    internal sealed record Request(
        string Name,
        string? Description,
        Guid TrainerId,
        ClassScheduleDto Schedule,
        int Capacity,
        bool WaitlistEnabled);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/classes", async (Guid gymId, Request request, ISender sender) =>
        {
            var command = new CreateClassCommand(
                gymId,
                request.Name,
                request.Description,
                request.TrainerId,
                request.Schedule,
                request.Capacity,
                request.WaitlistEnabled);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Create a new gym class")
        .WithDescription("Creates a scheduled gym class with a trainer, capacity, and recurring schedule. Waitlist can be enabled when capacity is full.")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
