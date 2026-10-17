using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Queries.GetCheckInHistory;

internal sealed class GetCheckInHistoryQueryHandler : IQueryHandler<GetCheckInHistoryQuery, PagedResponse<CheckInDto>>
{
    private readonly IGymQueryService _gymQueryService;

    public GetCheckInHistoryQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<PagedResponse<CheckInDto>>> Handle(GetCheckInHistoryQuery request, CancellationToken cancellationToken)
    {
        var result = await _gymQueryService.GetCheckInHistoryAsync(
            new GymId(request.GymId),
            request.From,
            request.To,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}
