using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.DeleteGym;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class DeleteGymEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/admin/gyms/{gymId:guid}", async (Guid gymId, ISender sender) =>
        {
            var command = new DeleteGymCommand(gymId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Admin", "Gyms")
        .WithSummary("Hard-delete a gym")
        .WithDescription("Permanently deletes the gym: drops the per-gym schemas (g{N}_*), removes the gyms_registry row, and clears identity gym memberships. Irreversible. Requires platform_admin role.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization("AdminPolicy");
    }
}
