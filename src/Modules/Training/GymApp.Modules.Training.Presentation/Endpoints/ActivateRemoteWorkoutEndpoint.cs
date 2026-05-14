using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.Commands.ActivateRemoteWorkout;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class ActivateRemoteWorkoutEndpoint : IEndpoint
{
    internal sealed record Request(
        Guid ClientId,
        Guid GymId,
        string? Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/training/remote-workout/activate", async (Request request, HttpContext httpContext, ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var command = new ActivateRemoteWorkoutCommand(
                trainerId,
                request.ClientId,
                request.GymId,
                request.Reason);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("Activate remote workout for a client")
        .WithDescription("Enables remote workout mode for a specific client. Trainer-only operation.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization();
    }
}
