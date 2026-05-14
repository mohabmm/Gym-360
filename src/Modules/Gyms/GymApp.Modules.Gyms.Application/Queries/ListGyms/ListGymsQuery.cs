using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.ListGyms;

public sealed record ListGymsQuery(
    int Page,
    int PageSize,
    string? Name) : IQuery<PagedResponse<GymResponse>>;
