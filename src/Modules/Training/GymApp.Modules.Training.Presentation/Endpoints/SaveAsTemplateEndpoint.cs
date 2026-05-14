using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.Commands.SaveAsTemplate;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class SaveAsTemplateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/training/workouts/{workoutId:guid}/save-template", async (Guid workoutId, ISender sender) =>
        {
            var command = new SaveAsTemplateCommand(workoutId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("Save an existing workout as a template")
        .WithDescription("Marks an existing workout as a reusable template. Requires workout_builder feature enabled on gym plan.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
