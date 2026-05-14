using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.Commands.CreateExercise;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class CreateExerciseEndpoint : IEndpoint
{
    internal sealed record Request(
        string Name,
        string? NameAr,
        string? Description,
        string? Instructions,
        string? DemoMediaUrl,
        List<string> MuscleGroups,
        string Equipment,
        string Category,
        string Scope,
        Guid? GymId,
        Guid? CreatedBy);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/training/exercises", async (Request request, ISender sender) =>
        {
            var command = new CreateExerciseCommand(
                request.Name,
                request.NameAr,
                request.Description,
                request.Instructions,
                request.DemoMediaUrl,
                request.MuscleGroups,
                request.Equipment,
                request.Category,
                request.Scope,
                request.GymId,
                request.CreatedBy);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("Create a new exercise")
        .WithDescription("Creates a new exercise with muscle groups and equipment. Requires workout_builder feature enabled on gym plan.")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization();
    }
}
