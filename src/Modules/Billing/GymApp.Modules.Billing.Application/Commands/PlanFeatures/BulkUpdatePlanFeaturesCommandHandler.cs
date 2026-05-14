using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.PlanFeatures;

internal sealed class BulkUpdatePlanFeaturesCommandHandler : ICommandHandler<BulkUpdatePlanFeaturesCommand>
{
    private readonly IPlanFeatureRepository _planFeatureRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public BulkUpdatePlanFeaturesCommandHandler(
        IPlanFeatureRepository planFeatureRepository,
        ISubscriptionPlanRepository planRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _planFeatureRepository = planFeatureRepository;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(BulkUpdatePlanFeaturesCommand request, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure(BillingErrors.PlanNotFound);

        foreach (var toggle in request.Features)
        {
            var existing = await _planFeatureRepository.GetByPlanAndFeatureAsync(
                request.PlanId, toggle.FeatureId, cancellationToken);

            if (existing is not null)
            {
                existing.Update(toggle.IsEnabled, toggle.LimitValue);
                _planFeatureRepository.Update(existing);
            }
            else
            {
                var planFeature = PlanFeature.Create(
                    request.PlanId,
                    toggle.FeatureId,
                    toggle.IsEnabled,
                    toggle.LimitValue);

                await _planFeatureRepository.AddAsync(planFeature, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
