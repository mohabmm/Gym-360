using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.ReactivateGym;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class ReactivateGymEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/admin/gyms/{gymId:guid}/reactivate", async (Guid gymId, ISender sender) =>
        {
            var command = new ReactivateGymCommand(gymId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Admin", "Gyms")
        .WithSummary("Reactivate a suspended gym")
        .WithDescription("Lifts an active suspension and returns the gym to Active status. Requires platform_admin role. Member access is immediately restored.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization("AdminPolicy");
    }
}
