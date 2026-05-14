using GymApp.Common.Presentation;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Application.Queries.GetClientProgress;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Progress.Presentation.Endpoints;

internal sealed class GetClientProgressEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/progress/clients/{clientId:guid}", async (
            Guid clientId,
            Guid trainerId,
            Guid gymId,
            ISender sender) =>
        {
            var query = new GetClientProgressQuery(trainerId, clientId, gymId);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Progress")
        .WithSummary("Get client progress summary")
        .WithDescription("Returns a trainer's view of a client's progress, including latest metrics, recent metric history, photo count, and workout/check-in streaks.")
        .Produces<ApiResponse<ClientProgressResponse>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
