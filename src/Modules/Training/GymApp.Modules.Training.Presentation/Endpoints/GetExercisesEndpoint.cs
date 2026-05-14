using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Application.Queries.GetExercises;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class GetExercisesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/training/exercises", async (
            Guid? gymId,
            string? muscleGroup,
            string? equipment,
            string? search,
            int page,
            int pageSize,
            ISender sender) =>
        {
            var query = new GetExercisesQuery(gymId, muscleGroup, equipment, search, page, pageSize);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("List exercises with filters and pagination")
        .WithDescription("Returns a paginated list of exercises filterable by muscle group, equipment, and search term. Public within the gym context.")
        .Produces<ApiResponse<PagedResponse<ExerciseDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();
    }
}
