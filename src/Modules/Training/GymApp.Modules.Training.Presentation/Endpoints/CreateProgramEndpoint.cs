using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.Commands.CreateProgram;
using GymApp.Modules.Training.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class CreateProgramEndpoint : IEndpoint
{
    internal sealed record Request(
        Guid GymId,
        string Name,
        string? NameAr,
        string? Description,
        int DurationWeeks,
        List<ProgramWeekDto> Weeks,
        bool IsTemplate);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/training/programs", async (Request request, HttpContext httpContext, ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var command = new CreateProgramCommand(
                trainerId,
                request.GymId,
                request.Name,
                request.NameAr,
                request.Description,
                request.DurationWeeks,
                request.Weeks,
                request.IsTemplate);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("Create a multi-week training program")
        .WithDescription("Creates a structured multi-week program with weekly workout schedules. Requires workout_builder feature enabled on gym plan.")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization();
    }
}
