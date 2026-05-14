using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.ChangePassword;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class ChangePasswordEndpoint : IEndpoint
{
    internal sealed record Request(string CurrentPassword, string NewPassword);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/identity/change-password", async (Request request, HttpContext httpContext, ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithTags("Identity")
        .WithSummary("Change current user's password")
        .WithDescription("Authenticated user changes their own password by supplying the current password plus the new password. Use this flow for first-login default password rotation (initial password = 123456 for staff-created users) and any subsequent change-from-settings flow. For forgotten passwords, use POST /identity/forgot-password + /identity/reset-password.")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);
    }
}
