using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.Commands.MarkNotificationRead;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Communication.Presentation.Endpoints.Notifications;

internal sealed class MarkNotificationReadEndpoint : IEndpoint
{
    internal sealed record MarkNotificationReadRequest(Guid UserId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/communication/notifications/{notificationId:guid}/read", async (
            Guid notificationId,
            MarkNotificationReadRequest request,
            ISender sender) =>
        {
            var command = new MarkNotificationReadCommand(notificationId, request.UserId);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Mark notification as read")
        .WithDescription("Marks a single notification as read for the specified user.")
        .Produces(204)
        .WithTags("Communication")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404);
    }
}
