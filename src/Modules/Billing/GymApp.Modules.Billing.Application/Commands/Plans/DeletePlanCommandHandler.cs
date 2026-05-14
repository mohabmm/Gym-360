using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Commands.Plans;

// Soft-delete: deactivates the plan so existing gym_subscriptions keep their FK, but the plan
// can no longer be selected for new assignments. Hard deletion would break historical records.
internal sealed class DeletePlanCommandHandler : ICommandHandler<DeletePlanCommand>
{
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public DeletePlanCommandHandler(
        ISubscriptionPlanRepository planRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeletePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure(BillingErrors.PlanNotFound);

        if (!plan.IsActive)
            return Result.Success();

        plan.Deactivate();
        _planRepository.Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
