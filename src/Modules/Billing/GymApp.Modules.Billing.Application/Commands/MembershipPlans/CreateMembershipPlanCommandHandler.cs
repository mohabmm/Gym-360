using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.MembershipPlans;

internal sealed class CreateMembershipPlanCommandHandler : ICommandHandler<CreateMembershipPlanCommand, Guid>
{
    private readonly IMembershipPlanRepository _membershipPlanRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public CreateMembershipPlanCommandHandler(
        IMembershipPlanRepository membershipPlanRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _membershipPlanRepository = membershipPlanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateMembershipPlanCommand request, CancellationToken cancellationToken)
    {
        var price = Money.Create(request.Price);

        var plan = MembershipPlan.Create(
            request.GymId,
            request.Name,
            request.DurationDays,
            price,
            request.Features);

        await _membershipPlanRepository.AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(plan.Id);
    }
}
