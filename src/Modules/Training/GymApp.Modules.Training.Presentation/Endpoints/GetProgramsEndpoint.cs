using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Application.Queries.GetPrograms;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class GetProgramsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/training/programs", async (Guid trainerId, Guid gymId, ISender sender) =>
        {
            var query = new GetProgramsQuery(trainerId, gymId);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("List training programs for a trainer")
        .WithDescription("Returns all training programs created by a trainer within a specific gym.")
        .Produces<ApiResponse<List<ProgramDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();
    }
}
