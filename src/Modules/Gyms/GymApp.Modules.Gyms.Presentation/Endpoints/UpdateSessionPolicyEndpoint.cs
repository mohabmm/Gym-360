using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.UpdateSessionPolicy;
using GymApp.Modules.Gyms.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class UpdateSessionPolicyEndpoint : IEndpoint
{
    internal sealed record Request(
        int CancellationWindowHours,
        int NoshowThreshold,
        int NoshowWindowDays,
        NoshowAction NoshowAction);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/gyms/{gymId:guid}/session-policy", async (Guid gymId, Request request, ISender sender) =>
        {
            var command = new UpdateSessionPolicyCommand(
                gymId,
                request.CancellationWindowHours,
                request.NoshowThreshold,
                request.NoshowWindowDays,
                request.NoshowAction.ToString());

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Update gym session cancellation policy")
        .WithDescription("Configures the gym's session policy including the late-cancellation window in hours, no-show threshold, no-show window, and the action taken on no-shows. Policy is per-gym and configurable.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
