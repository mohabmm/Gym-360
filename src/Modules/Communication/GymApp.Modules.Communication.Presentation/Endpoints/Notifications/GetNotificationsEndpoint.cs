using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.DTOs;
using GymApp.Modules.Communication.Application.Queries.GetNotifications;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Communication.Presentation.Endpoints.Notifications;

internal sealed class GetNotificationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/communication/notifications", async (
            Guid userId,
            Guid? gymId,
            int page,
            int pageSize,
            ISender sender) =>
        {
            var query = new GetNotificationsQuery(userId, gymId, page, pageSize);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get user notifications")
        .WithDescription("Returns a paginated list of notifications for the specified user, optionally scoped to a gym.")
        .WithTags("Communication")
        .Produces<ApiResponse<PagedResponse<NotificationDto>>>(200)
        .Produces<ApiResponse<object>>(401);
    }
}
