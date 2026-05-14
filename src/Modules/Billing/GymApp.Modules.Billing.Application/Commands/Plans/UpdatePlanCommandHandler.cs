using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.Plans;

internal sealed class UpdatePlanCommandHandler : ICommandHandler<UpdatePlanCommand>
{
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public UpdatePlanCommandHandler(
        ISubscriptionPlanRepository planRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);

        if (plan is null)
        {
            return Result.Failure(BillingErrors.PlanNotFound);
        }

        plan.Update(
            request.Name ?? plan.Name,
            request.Description ?? plan.Description,
            request.MonthlyPrice ?? plan.MonthlyPriceEgp,
            request.AnnualPrice ?? plan.AnnualPriceEgp,
            request.MemberLimit ?? plan.MemberLimit,
            request.TrainerLimit ?? plan.TrainerLimit,
            request.StaffLimit ?? plan.StaffLimit,
            request.IsPublic ?? plan.IsPublic,
            request.SortOrder ?? plan.SortOrder);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                plan.Activate();
            else
                plan.Deactivate();
        }

        _planRepository.Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
