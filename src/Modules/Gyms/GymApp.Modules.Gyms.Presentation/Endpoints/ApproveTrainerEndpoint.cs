using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.ApproveTrainer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class ApproveTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/trainers/{trainerId:guid}/approve", async (Guid gymId, Guid trainerId, ISender sender) =>
        {
            var command = new ApproveTrainerCommand(gymId, trainerId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Approve a trainer request")
        .WithDescription("Approves a pending trainer registration request, granting the trainer access to the gym.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
