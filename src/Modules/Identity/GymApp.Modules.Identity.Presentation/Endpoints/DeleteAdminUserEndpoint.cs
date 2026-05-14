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
        .WithSummary("Delete an admin user")
        .WithDescription("Soft-deletes a user and removes any admin role they hold. Callers cannot delete themselves. Requires platform_admin.")
        .Produces(204)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization("platform_admin");
    }
}
