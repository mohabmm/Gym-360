using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.Commands.MarkNotificationRead;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Communication.Presentation.Endpoints.Notifications;

internal sealed class MarkNotificationReadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/communication/notifications/{notificationId:guid}/read", async (
            HttpContext httpContext,
            Guid notificationId,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            var command = new MarkNotificationReadCommand(notificationId, userId);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Mark notification as read")
        .WithDescription("Marks a single notification as read for the authenticated user. User id is derived from the JWT.")
        .Produces(204)
        .WithTags("Communication")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404);
    }
}
