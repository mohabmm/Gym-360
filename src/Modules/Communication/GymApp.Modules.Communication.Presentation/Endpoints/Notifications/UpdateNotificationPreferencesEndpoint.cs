using System.Security.Claims;
using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.Commands.UpdateNotificationPreferences;
using GymApp.Modules.Communication.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Communication.Presentation.Endpoints.Notifications;

internal sealed class UpdateNotificationPreferencesEndpoint : IEndpoint
{
    // Caller's userId is derived from the JWT 'sub' claim — never accepted in the body
    // so a member can only update their own preferences.
    internal sealed record UpdateNotificationPreferencesRequest(
        Guid? GymId,
        List<NotificationPreferenceDto> Preferences,
        TimeOnly? QuietHoursStart,
        TimeOnly? QuietHoursEnd);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/communication/notification-preferences", async (
            UpdateNotificationPreferencesRequest request,
            HttpContext http,
            ISender sender) =>
        {
            var sub = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? http.User.FindFirstValue("sub");
            if (!Guid.TryParse(sub, out var userId) || userId == Guid.Empty)
                return ApiResults.Problem(new Error("Auth.UserMissing", "Authenticated user id is missing."));

            var command = new UpdateNotificationPreferencesCommand(
                userId,
                request.GymId,
                request.Preferences,
                request.QuietHoursStart,
                request.QuietHoursEnd);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Update notification preferences")
        .WithDescription("Updates notification channel preferences and quiet hours for a user, optionally scoped to a gym.")
        .Produces(204)
        .WithTags("Communication")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }
}
