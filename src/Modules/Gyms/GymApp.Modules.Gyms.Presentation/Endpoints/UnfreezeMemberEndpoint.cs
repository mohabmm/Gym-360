using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.UnfreezeMember;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class UnfreezeMemberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/members/{memberId:guid}/unfreeze", async (Guid gymId, Guid memberId, ISender sender) =>
        {
            var command = new UnfreezeMemberCommand(gymId, memberId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Unfreeze a member's membership")
        .WithDescription("Restores a frozen member's membership, allowing them to check in again.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization()
        .RequireMemberAccess(MemberAccessLevel.Staff);
    }
}
