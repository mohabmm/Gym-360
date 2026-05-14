using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetAnnouncements;

internal sealed class GetAnnouncementsQueryHandler : IQueryHandler<GetAnnouncementsQuery, PagedResponse<AnnouncementDto>>
{
    private readonly IGymQueryService _gymQueryService;

    public GetAnnouncementsQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<PagedResponse<AnnouncementDto>>> Handle(GetAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        var result = await _gymQueryService.GetAnnouncementsAsync(
            request.GymId,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}
