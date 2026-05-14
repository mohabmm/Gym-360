using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.DTOs;
using GymApp.Modules.Communication.Application.Queries.GetUnreadCount;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Communication.Presentation.Endpoints.Notifications;

internal sealed class GetUnreadCountEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/communication/notifications/unread-count", async (
            Guid userId,
            Guid? gymId,
            ISender sender) =>
        {
            var query = new GetUnreadCountQuery(userId, gymId);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get unread counts")
        .WithDescription("Returns the count of unread notifications and messages for a user, optionally scoped to a gym.")
        .WithTags("Communication")
        .Produces<ApiResponse<UnreadCountResponse>>(200)
        .Produces<ApiResponse<object>>(401);
    }
}
