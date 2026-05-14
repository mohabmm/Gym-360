using GymApp.Common.Presentation;
using GymApp.Modules.Progress.Application.Commands.UploadProgressPhoto;
using GymApp.Modules.Progress.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Progress.Presentation.Endpoints;

internal sealed class UploadProgressPhotoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/progress/photos", async (UploadProgressPhotoRequest request, HttpContext httpContext, ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            var command = new UploadProgressPhotoCommand(
                userId,
                request.GymId,
                request.PhotoUrl,
                request.ThumbnailUrl,
                request.Angle,
                request.Date,
                request.Notes);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Progress")
        .WithSummary("Upload progress photo")
        .WithDescription("Uploads a member progress photo. Requires the progress_photos feature to be enabled on the gym's subscription plan. Subject to per-plan photo count storage limits.")
        .Produces<ApiResponse<Guid>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
        .RequireAuthorization();
    }
}

internal sealed record UploadProgressPhotoRequest(
    Guid GymId,
    string PhotoUrl,
    string ThumbnailUrl,
    string Angle,
    DateOnly Date,
    string? Notes);
