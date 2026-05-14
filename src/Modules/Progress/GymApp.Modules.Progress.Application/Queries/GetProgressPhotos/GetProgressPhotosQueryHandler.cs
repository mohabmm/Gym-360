using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Application.Mappers;
using GymApp.Modules.Progress.Domain.Repositories;

namespace GymApp.Modules.Progress.Application.Queries.GetProgressPhotos;

internal sealed class GetProgressPhotosQueryHandler : IQueryHandler<GetProgressPhotosQuery, PagedResponse<ProgressPhotoDto>>
{
    private readonly IProgressPhotoRepository _repository;

    public GetProgressPhotosQueryHandler(IProgressPhotoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResponse<ProgressPhotoDto>>> Handle(GetProgressPhotosQuery request, CancellationToken cancellationToken)
    {
        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var photos = await _repository.GetByUserAsync(
            request.UserId, request.GymId, page, pageSize, cancellationToken);

        int totalCount = await _repository.GetCountAsync(
            request.UserId, request.GymId, cancellationToken);

        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var dtos = photos.Select(ProgressMapper.ToDto).ToList();

        var response = new PagedResponse<ProgressPhotoDto>(
            dtos, page, pageSize, totalCount, totalPages);

        return Result.Success(response);
    }
}
