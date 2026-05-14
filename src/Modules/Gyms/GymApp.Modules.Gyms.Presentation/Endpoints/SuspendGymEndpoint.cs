using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.SuspendGym;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class SuspendGymEndpoint : IEndpoint
{
    internal sealed record Request(string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/admin/gyms/{gymId:guid}/suspend", async (Guid gymId, Request request, ISender sender) =>
        {
            var command = new SuspendGymCommand(gymId, request.Reason);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Admin", "Gyms")
        .WithSummary("Suspend an active gym")
        .WithDescription("Suspends the gym, blocking member access and new bookings. Requires platform_admin role. A suspension reason must be provided.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization("AdminPolicy");
    }
}
