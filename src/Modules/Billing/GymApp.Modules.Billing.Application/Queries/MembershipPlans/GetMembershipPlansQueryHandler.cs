using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.MembershipPlans;

internal sealed class GetMembershipPlansQueryHandler : IQueryHandler<GetMembershipPlansQuery, List<MembershipPlanDto>>
{
    private readonly IMembershipPlanRepository _membershipPlanRepository;

    public GetMembershipPlansQueryHandler(IMembershipPlanRepository membershipPlanRepository)
    {
        _membershipPlanRepository = membershipPlanRepository;
    }

    public async Task<Result<List<MembershipPlanDto>>> Handle(GetMembershipPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _membershipPlanRepository.GetByGymIdAsync(request.GymId, cancellationToken);

        var dtos = plans.Select(p => new MembershipPlanDto(
            p.Id,
            p.GymId,
            p.Name,
            p.DurationDays,
            p.Price.Amount,
            p.Price.Currency,
            p.Features.ToList(),
            p.IsActive,
            p.SortOrder,
            p.CreatedAt)).ToList();

        return Result.Success(dtos);
    }
}
