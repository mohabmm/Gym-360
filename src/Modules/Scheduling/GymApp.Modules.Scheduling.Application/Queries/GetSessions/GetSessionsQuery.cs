using GymApp.Common.Application.Messaging;
using GymApp.Modules.Scheduling.Application.DTOs;

namespace GymApp.Modules.Scheduling.Application.Queries.GetSessions;

public sealed record GetSessionsQuery(
    Guid GymId,
    Guid? TrainerId,
    Guid? MemberId,
    DateOnly? From,
    DateOnly? To,
    string? Status,
    int Page,
    int PageSize) : IQuery<PagedResponse<SessionDto>>;
