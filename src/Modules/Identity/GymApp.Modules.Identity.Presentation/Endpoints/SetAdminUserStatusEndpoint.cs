using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.AdminUsers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class SetAdminUserStatusEndpoint : IEndpoint
{
    internal sealed record Request(string Status);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("api/v1/admin/users/{userId:guid}/status",
            async (Guid userId, Request request, ISender sender, HttpContext httpContext) =>
            {
                Guid? requestedBy = UserContext.TryGetUserId(httpContext, out var me) ? me : null;
                var command = new SetAdminUserStatusCommand(userId, requestedBy, request.Status?.Trim().ToLowerInvariant() ?? string.Empty);

                var result = await sender.Send(command);

                return result.IsSuccess
                    ? Results.Ok(new ApiResponse<object>(null))
                    : ApiResults.Problem(result.Error);
            })
            .WithTags("Admin", "Identity")
            .WithSummary("Suspend or reactivate a user")
            .WithDescription("Status 'suspended' locks the user out immediately (refresh tokens revoked, current access token rejected). 'active' reactivates. Callers cannot change their own status. Requires platform_admin.")
            .Produces<ApiResponse<object>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(409)
            .RequireAuthorization("platform_admin");
    }
}
