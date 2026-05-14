using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Application.Queries.GetRemoteWorkoutStatus;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class GetRemoteWorkoutStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/training/remote-workout/status", async (Guid clientId, Guid gymId, ISender sender) =>
        {
            var query = new GetRemoteWorkoutStatusQuery(clientId, gymId);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("Get remote workout status for a client")
        .WithDescription("Returns whether remote workout mode is active for a client, including trainer and activation details.")
        .Produces<ApiResponse<RemoteWorkoutStatusDto>>(200)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();
    }
}
