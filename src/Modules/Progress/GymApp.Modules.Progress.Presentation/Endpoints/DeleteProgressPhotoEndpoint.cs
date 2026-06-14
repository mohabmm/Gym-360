using GymApp.Common.Domain;
using GymApp.Common.Presentation;
using GymApp.Modules.Progress.Application.Commands.DeleteProgressPhoto;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Progress.Presentation.Endpoints;

internal sealed class DeleteProgressPhotoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/progress/photos/{photoId:guid}", async (
            Guid photoId,
            HttpContext http,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(http, out var userId))
                return ApiResults.Problem(new Error("Auth.UserMissing", "Authenticated user id is missing."));

            var command = new DeleteProgressPhotoCommand(userId, photoId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Progress")
        .WithSummary("Delete a progress photo")
        .WithDescription("Permanently deletes a member's progress photo by ID. Only the owning member may delete their own photos.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
