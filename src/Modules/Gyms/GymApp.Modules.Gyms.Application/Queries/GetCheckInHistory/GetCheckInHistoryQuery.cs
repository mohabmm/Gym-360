using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetCheckInHistory;

public sealed record GetCheckInHistoryQuery(
    Guid GymId,
    DateTime? From,
    DateTime? To,
    int Page,
    int PageSize) : IQuery<PagedResponse<CheckInDto>>;
