using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.Plans;

internal sealed class GetAllPlansQueryHandler : IQueryHandler<GetAllPlansQuery, List<SubscriptionPlanDto>>
{
    private readonly ISubscriptionPlanRepository _planRepository;

    public GetAllPlansQueryHandler(ISubscriptionPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<Result<List<SubscriptionPlanDto>>> Handle(GetAllPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _planRepository.GetAllActiveAsync(cancellationToken);

        var dtos = plans.Select(p => new SubscriptionPlanDto(
            p.Id,
            p.Code,
            p.Name,
            p.Description,
            p.MonthlyPriceEgp,
            p.AnnualPriceEgp,
            p.MemberLimit,
            p.TrainerLimit,
            p.StaffLimit,
            p.IsActive,
            p.IsPublic,
            p.SortOrder,
            p.CreatedAt,
            p.UpdatedAt)).ToList();

        return Result.Success(dtos);
    }
}
