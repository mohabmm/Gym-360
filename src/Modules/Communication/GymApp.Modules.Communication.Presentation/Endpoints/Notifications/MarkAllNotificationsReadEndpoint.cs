using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.Commands.MarkAllNotificationsRead;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Communication.Presentation.Endpoints.Notifications;

internal sealed class MarkAllNotificationsReadEndpoint : IEndpoint
{
    internal sealed record MarkAllNotificationsReadRequest(Guid UserId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/communication/notifications/read-all", async (
            MarkAllNotificationsReadRequest request,
            ISender sender) =>
        {
            var command = new MarkAllNotificationsReadCommand(request.UserId);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Mark all notifications read")
        .WithDescription("Marks all unread notifications as read for the specified user.")
        .Produces(204)
        .WithTags("Communication")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }
}
