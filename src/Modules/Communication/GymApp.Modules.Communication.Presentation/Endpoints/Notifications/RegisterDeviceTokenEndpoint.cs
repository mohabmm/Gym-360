using System.Security.Claims;
using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.Commands.RegisterDeviceToken;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Communication.Presentation.Endpoints.Notifications;

internal sealed class RegisterDeviceTokenEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/notifications/device-tokens", async (
            RegisterDeviceTokenRequest request,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                ?? user.FindFirst("sub");

            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return ApiResults.Problem("Auth.InvalidToken", "Unable to determine user identity from token.", 401);

            var command = new RegisterDeviceTokenCommand(userId, request.Token, request.Platform);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Register FCM device token")
        .WithDescription("Registers or updates an FCM device token for push notification delivery to the authenticated user.")
        .Produces(204)
        .WithTags("Communication")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }

    internal sealed record RegisterDeviceTokenRequest(string Token, string Platform);
}
