using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.AddTrainer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class AddTrainerEndpoint : IEndpoint
{
    internal sealed record Request(
        Guid TrainerId,
        string? Bio = null,
        IReadOnlyList<string>? Specializations = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/trainers", async (Guid gymId, Request request, ISender sender) =>
        {
            var command = new AddTrainerCommand(gymId, request.TrainerId, request.Bio, request.Specializations);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Add a trainer to a gym")
        .WithDescription("Associates a trainer with a gym. Trainers can work at multiple gyms simultaneously per platform policy.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
