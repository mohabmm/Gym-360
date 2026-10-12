using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.RemoveMember;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class RemoveMemberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/gyms/{gymId:guid}/members/{memberId:guid}", async (Guid gymId, Guid memberId, ISender sender) =>
        {
            var command = new RemoveMemberCommand(gymId, memberId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Remove a member from a gym")
        .WithDescription("Unenrolls a member from the gym. Active memberships are terminated. Owner / platform admin only. Returns 204 No Content on success.")
        .Produces(204)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization("gym_owner_or_admin")
        .RequireMemberAccess(MemberAccessLevel.Staff);
    }
}
