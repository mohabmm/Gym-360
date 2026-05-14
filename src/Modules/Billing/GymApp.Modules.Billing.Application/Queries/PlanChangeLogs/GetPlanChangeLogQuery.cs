using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.PlanChangeLogs;

public sealed record GetPlanChangeLogQuery(
    Guid? GymId,
    int Page,
    int PageSize) : IQuery<PagedResponse<PlanChangeLogDto>>;
