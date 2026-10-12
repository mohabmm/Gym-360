using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.AddMember;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class AddMemberEndpoint : IEndpoint
{
    internal sealed record Request(
        Guid UserId,
        string? Section,
        string? MemberNumber);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/members", async (Guid gymId, Request request, ISender sender) =>
        {
            var command = new AddMemberCommand(
                gymId,
                request.UserId,
                request.Section,
                request.MemberNumber);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Add a member to a gym")
        .WithDescription("Enrolls a user as a gym member. Optionally assigns them to a gym section (Men's/Women's/Mixed) and a member number.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization()
        .RequireMemberAccess(MemberAccessLevel.Staff);
    }
}
