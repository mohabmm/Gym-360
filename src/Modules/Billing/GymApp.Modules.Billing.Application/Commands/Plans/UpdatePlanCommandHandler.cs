using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
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

        plan.Update(MergeDetails(plan, request));
        ApplyActiveState(plan, request.IsActive);

        _planRepository.Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>Request fields win; anything omitted keeps the plan's current value.</summary>
    private static SubscriptionPlanDetails MergeDetails(SubscriptionPlan plan, UpdatePlanCommand request)
    {
        var details = new SubscriptionPlanDetails
        {
            Code = plan.Code,
            Name = request.Name ?? plan.Name,
            Description = request.Description ?? plan.Description,
            MonthlyPriceEgp = request.MonthlyPrice ?? plan.MonthlyPriceEgp,
            AnnualPriceEgp = request.AnnualPrice ?? plan.AnnualPriceEgp,
            MemberLimit = plan.MemberLimit,
            TrainerLimit = plan.TrainerLimit,
            StaffLimit = plan.StaffLimit,
            IsPublic = request.IsPublic ?? plan.IsPublic,
            SortOrder = request.SortOrder ?? plan.SortOrder
        };

        return MergeLimits(details, request);
    }

    private static SubscriptionPlanDetails MergeLimits(SubscriptionPlanDetails details, UpdatePlanCommand request) =>
        details with
        {
            MemberLimit = request.MemberLimit ?? details.MemberLimit,
            TrainerLimit = request.TrainerLimit ?? details.TrainerLimit,
            StaffLimit = request.StaffLimit ?? details.StaffLimit
        };

    private static void ApplyActiveState(SubscriptionPlan plan, bool? isActive)
    {
        if (isActive is null)
            return;

        if (isActive.Value)
            plan.Activate();
        else
            plan.Deactivate();
    }
}
