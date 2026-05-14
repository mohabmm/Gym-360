using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.RegisterGym;
using GymApp.Modules.Gyms.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class RegisterGymEndpoint : IEndpoint
{
    internal sealed record Request(
        Guid OwnerId,
        string Name,
        string? Description,
        AddressDto Address,
        ContactInfoDto Contact,
        bool? AutoApprove);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms", async (Request request, ISender sender) =>
        {
            var command = new RegisterGymCommand(
                request.OwnerId,
                request.Name,
                request.Description,
                request.Address,
                request.Contact,
                request.AutoApprove ?? true);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Register a new gym")
        .WithDescription("Creates a new gym registration pending admin approval. The gym starts in Pending status and must be approved before members can join.")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization("platform_admin");
    }
}
