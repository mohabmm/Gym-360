using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.Plans;

internal sealed class CreatePlanCommandHandler : ICommandHandler<CreatePlanCommand, Guid>
{
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public CreatePlanCommandHandler(
        ISubscriptionPlanRepository planRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        var existing = await _planRepository.GetByCodeAsync(request.Code, cancellationToken);

        if (existing is not null)
        {
            return Result.Failure<Guid>(BillingErrors.PlanCodeAlreadyExists);
        }

        var plan = SubscriptionPlan.Create(new SubscriptionPlanDetails
        {
            Code = request.Code,
            Name = request.Name,
            MonthlyPriceEgp = request.MonthlyPrice,
            AnnualPriceEgp = request.AnnualPrice,
            MemberLimit = request.MemberLimit,
            TrainerLimit = request.TrainerLimit,
            StaffLimit = request.StaffLimit,
            Description = request.Description,
            IsPublic = request.IsPublic,
            SortOrder = request.SortOrder
        });

        await _planRepository.AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(plan.Id);
    }
}
