using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.UpdateTrainerProfile;
using GymApp.Modules.Gyms.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class UpdateTrainerProfileEndpoint : IEndpoint
{
    internal sealed record Request(string? Bio, IReadOnlyList<string>? Specializations);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/gyms/{gymId:guid}/trainers/{trainerId:guid}/profile",
            async (Guid gymId, Guid trainerId, Request request, ISender sender) =>
            {
                var command = new UpdateTrainerProfileCommand(
                    gymId,
                    trainerId,
                    request.Bio,
                    request.Specializations);

                var result = await sender.Send(command);

                return result.IsSuccess
                    ? Results.Ok(new ApiResponse<object>(null))
                    : ApiResults.Problem(result.Error);
            })
        .WithTags("Gyms")
        .WithSummary("Update trainer profile")
        .WithDescription("Updates bio and/or specializations for a trainer at a specific gym. Either field can be omitted to leave it unchanged.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
