using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.Commands.CreateWorkout;
using GymApp.Modules.Training.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class CreateWorkoutEndpoint : IEndpoint
{
    internal sealed record Request(
        Guid GymId,
        string Name,
        string? NameAr,
        string? Description,
        List<WorkoutExerciseDto> Exercises,
        int? EstimatedDuration,
        bool IsTemplate);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/training/workouts", async (Request request, HttpContext httpContext, ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var command = new CreateWorkoutCommand(
                trainerId,
                request.GymId,
                request.Name,
                request.NameAr,
                request.Description,
                request.Exercises,
                request.EstimatedDuration,
                request.IsTemplate);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("Create a new workout")
        .WithDescription("Creates a workout with a list of exercises and sets configuration. Requires workout_builder feature enabled on gym plan. Note: exercises[].reps is a STRING (max 50 chars) to support rep schemes beyond a single count — examples: \"10\", \"8-12\", \"AMRAP\", \"30s\", \"To failure\".")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization();
    }
}
