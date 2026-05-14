using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.AddStaff;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class AddStaffEndpoint : IEndpoint
{
    internal sealed record Request(Guid UserId, string Role);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/staff", async (Guid gymId, Request request, ISender sender) =>
        {
            var command = new AddStaffCommand(gymId, request.UserId, request.Role);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Add a staff member to a gym")
        .WithDescription("Assigns a user a staff role at the gym. Valid roles are defined by the gym's configured StaffRole list.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
