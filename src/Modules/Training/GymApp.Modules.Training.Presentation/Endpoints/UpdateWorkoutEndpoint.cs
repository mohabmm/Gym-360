using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.Commands.UpdateWorkout;
using GymApp.Modules.Training.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class UpdateWorkoutEndpoint : IEndpoint
{
    internal sealed record Request(
        string? Name,
        string? NameAr,
        string? Description,
        List<WorkoutExerciseDto>? Exercises,
        int? EstimatedDuration);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/training/workouts/{workoutId:guid}", async (Guid workoutId, Request request, ISender sender) =>
        {
            var command = new UpdateWorkoutCommand(
                workoutId,
                request.Name,
                request.NameAr,
                request.Description,
                request.Exercises,
                request.EstimatedDuration);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("Update an existing workout")
        .WithDescription("Updates name, description, exercises, or duration of an existing workout. Requires workout_builder feature enabled on gym plan.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
