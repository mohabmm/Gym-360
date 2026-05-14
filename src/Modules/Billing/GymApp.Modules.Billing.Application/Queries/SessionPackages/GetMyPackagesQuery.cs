using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.SessionPackages;

public sealed record GetMyPackagesQuery(
    Guid UserId,
    Guid GymId,
    string Status,
    int Page,
    int PageSize) : IQuery<PagedResponse<MySessionPackageDto>>;
