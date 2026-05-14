using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.UpdateGym;
using GymApp.Modules.Gyms.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class UpdateGymEndpoint : IEndpoint
{
    internal sealed record Request(
        string? Name,
        string? Description,
        AddressDto? Address,
        ContactInfoDto? Contact,
        List<string>? Amenities);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/gyms/{gymId:guid}", async (Guid gymId, Request request, ISender sender) =>
        {
            var command = new UpdateGymCommand(
                gymId,
                request.Name,
                request.Description,
                request.Address,
                request.Contact,
                request.Amenities);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Update gym details")
        .WithDescription("Updates gym name, description, address, contact info, or amenities. Only provided fields are updated.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization("gym_owner_or_admin");
    }
}
