using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.RejectGym;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class RejectGymEndpoint : IEndpoint
{
    internal sealed record Request(string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/admin/gyms/{gymId:guid}/reject", async (Guid gymId, Request request, ISender sender) =>
        {
            var command = new RejectGymCommand(gymId, request.Reason);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Admin", "Gyms")
        .WithSummary("Reject a pending gym")
        .WithDescription("Transitions the gym from Pending to Rejected status with a reason. Requires platform_admin role.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization("AdminPolicy");
    }
}
