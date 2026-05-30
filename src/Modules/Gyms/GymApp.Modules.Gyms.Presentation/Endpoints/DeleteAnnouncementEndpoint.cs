using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.DeleteAnnouncement;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class DeleteAnnouncementEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/gyms/{gymId:guid}/announcements/{announcementId:guid}", async (
            Guid gymId,
            Guid announcementId,
            ISender sender) =>
        {
            var command = new DeleteAnnouncementCommand(gymId, announcementId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Delete a gym announcement")
        .WithDescription("Permanently removes an announcement.")
        .Produces(204)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
