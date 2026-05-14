using GymApp.Common.Presentation;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Application.Queries.GetProgressPhotos;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Progress.Presentation.Endpoints;

internal sealed class GetProgressPhotosEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/progress/photos", async (
            Guid userId,
            Guid gymId,
            int page,
            int pageSize,
            ISender sender) =>
        {
            var query = new GetProgressPhotosQuery(userId, gymId, page, pageSize);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Progress")
        .WithSummary("Get progress photos")
        .WithDescription("Returns a paginated list of progress photos for a member, sorted by date descending.")
        .Produces<ApiResponse<PagedResponse<ProgressPhotoDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }
}
