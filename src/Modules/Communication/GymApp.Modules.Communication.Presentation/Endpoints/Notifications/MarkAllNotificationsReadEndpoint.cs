using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.Commands.MarkAllNotificationsRead;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Communication.Presentation.Endpoints.Notifications;

internal sealed class MarkAllNotificationsReadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/communication/notifications/read-all", async (
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            var command = new MarkAllNotificationsReadCommand(userId);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Mark all notifications read")
        .WithDescription("Marks all unread notifications as read for the authenticated user. User id is derived from the JWT.")
        .Produces(204)
        .WithTags("Communication")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }
}
