using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.DTOs;
using GymApp.Modules.Communication.Application.Queries.GetNotificationPreferences;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Communication.Presentation.Endpoints.Notifications;

internal sealed class GetNotificationPreferencesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/communication/notification-preferences", async (
            HttpContext httpContext,
            Guid? gymId,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            var query = new GetNotificationPreferencesQuery(userId, gymId);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get notification preferences")
        .WithDescription("Returns the notification preferences for the authenticated user, optionally scoped to a specific gym. User id is derived from the JWT; clients cannot request another user's preferences.")
        .WithTags("Communication")
        .Produces<ApiResponse<NotificationPreferencesResponse>>(200)
        .Produces<ApiResponse<object>>(401);
    }
}
