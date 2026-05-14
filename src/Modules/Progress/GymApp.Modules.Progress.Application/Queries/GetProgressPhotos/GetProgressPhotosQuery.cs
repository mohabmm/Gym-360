using GymApp.Common.Application.Messaging;
using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Modules.Progress.Application.Queries.GetProgressPhotos;

public sealed record GetProgressPhotosQuery(
    Guid UserId,
    Guid GymId,
    int Page,
    int PageSize) : IQuery<PagedResponse<ProgressPhotoDto>>;
