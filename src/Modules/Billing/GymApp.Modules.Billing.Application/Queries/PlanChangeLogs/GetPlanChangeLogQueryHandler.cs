using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.PlanChangeLogs;

internal sealed class GetPlanChangeLogQueryHandler : IQueryHandler<GetPlanChangeLogQuery, PagedResponse<PlanChangeLogDto>>
{
    private readonly IPlanChangeLogRepository _changeLogRepository;

    public GetPlanChangeLogQueryHandler(IPlanChangeLogRepository changeLogRepository)
    {
        _changeLogRepository = changeLogRepository;
    }

    public async Task<Result<PagedResponse<PlanChangeLogDto>>> Handle(GetPlanChangeLogQuery request, CancellationToken cancellationToken)
    {
        var logs = request.GymId.HasValue
            ? await _changeLogRepository.GetByGymIdAsync(request.GymId.Value, request.Page, request.PageSize, cancellationToken)
            : await _changeLogRepository.GetAllAsync(request.Page, request.PageSize, cancellationToken);

        var dtos = logs.Select(l => new PlanChangeLogDto(
            l.Id,
            l.GymId,
            l.ChangeType,
            l.EntityType,
            l.EntityId,
            l.OldValue,
            l.NewValue,
            l.Reason,
            l.ChangedBy,
            l.ChangedAt)).ToList();

        var totalCount = logs.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var response = new PagedResponse<PlanChangeLogDto>(
            dtos, request.Page, request.PageSize, totalCount, totalPages);

        return Result.Success(response);
    }
}
