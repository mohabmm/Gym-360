using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.Commands.AssignProgram;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class AssignProgramEndpoint : IEndpoint
{
    internal sealed record Request(
        Guid ClientId,
        Guid GymId,
        Guid ProgramId,
        DateTime StartDate,
        string? Notes);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/training/assignments/program", async (Request request, HttpContext httpContext, ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var command = new AssignProgramCommand(
                trainerId,
                request.ClientId,
                request.GymId,
                request.ProgramId,
                request.StartDate,
                request.Notes);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("Assign a training program to a client")
        .WithDescription("Assigns an existing program to a client starting on the given date. Requires workout_builder feature enabled on gym plan.")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization();
    }
}
