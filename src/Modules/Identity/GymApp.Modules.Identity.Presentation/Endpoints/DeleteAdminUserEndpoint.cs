using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.AdminUsers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class DeleteAdminUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/admin/users/{userId:guid}", async (Guid userId, ISender sender, HttpContext httpContext) =>
        {
            Guid? requestedBy = UserContext.TryGetUserId(httpContext, out var me) ? me : null;
            var command = new DeleteAdminUserCommand(userId, requestedBy);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Admin", "Identity")
        .WithSummary("Permanently delete a user")
        .WithDescription("Hard-deletes a user and erases their personal data across every module and gym (payment and earnings records are kept, anonymized). Blocked for yourself, the last super admin, and owners who still own gyms (409). Requires platform_admin.")
        .Produces<ApiResponse<object>>(409)
        .Produces(204)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization("platform_admin");
    }
}
