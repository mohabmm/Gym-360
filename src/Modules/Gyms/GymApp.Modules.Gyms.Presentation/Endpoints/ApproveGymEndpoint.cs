using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.ApproveGym;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class ApproveGymEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/admin/gyms/{gymId:guid}/approve", async (Guid gymId, ISender sender) =>
        {
            var command = new ApproveGymCommand(gymId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Admin", "Gyms")
        .WithSummary("Approve a pending gym")
        .WithDescription("Transitions the gym from Pending to Active status. Requires platform_admin role. Members can join and classes can be booked after approval.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization("AdminPolicy");
    }
}
