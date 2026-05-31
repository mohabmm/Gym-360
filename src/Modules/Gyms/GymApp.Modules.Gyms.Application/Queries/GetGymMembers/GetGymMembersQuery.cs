using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymMembers;

public sealed record GetGymMembersQuery(
    Guid GymId,
    int Page,
    int PageSize,
    string? Status,
    string? Section,
    Guid? TrainerId = null,
    string? Search = null) : IQuery<PagedResponse<MemberDto>>;
