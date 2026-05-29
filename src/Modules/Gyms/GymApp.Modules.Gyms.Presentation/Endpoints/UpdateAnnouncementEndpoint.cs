using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.UpdateAnnouncement;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class UpdateAnnouncementEndpoint : IEndpoint
{
    // The staff UI sends `body` for content on edit, or just `isActive` on the
    // publish/unpublish toggle. All fields are optional.
    internal sealed record Request(
        string? Title,
        string? Body,
        string? Content,
        string? Category,
        string? TargetAudience,
        bool? IsActive);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/gyms/{gymId:guid}/announcements/{announcementId:guid}", async (
            Guid gymId,
            Guid announcementId,
            Request request,
            ISender sender) =>
        {
            var command = new UpdateAnnouncementCommand(
                gymId,
                announcementId,
                request.Title,
                request.Body ?? request.Content,
                request.Category,
                request.TargetAudience,
                request.IsActive);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Update or publish/unpublish a gym announcement")
        .WithDescription("Edits an announcement's content and/or toggles its published state. Sending only isActive performs a publish/unpublish without changing content.")
        .Produces(204)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
