using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Application.Commands.MembershipPlans;

internal sealed class UpdateMembershipPlanCommandHandler : ICommandHandler<UpdateMembershipPlanCommand>
{
    private readonly IMembershipPlanRepository _planRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public UpdateMembershipPlanCommandHandler(
        IMembershipPlanRepository planRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateMembershipPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure(new Error("Billing.PlanNotFound", "Membership plan not found."));

        plan.Update(
            request.Name ?? plan.Name,
            request.DurationDays ?? plan.DurationDays,
            request.Price.HasValue ? Money.Create(request.Price.Value) : plan.Price,
            plan.SortOrder);

        if (request.Features is not null)
            plan.SetFeatures(request.Features);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value) plan.Activate(); else plan.Deactivate();
        }

        _planRepository.Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
