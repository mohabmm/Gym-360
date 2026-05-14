using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.PlanFeatures;

internal sealed class TogglePlanFeatureCommandHandler : ICommandHandler<TogglePlanFeatureCommand>
{
    private readonly IPlanFeatureRepository _planFeatureRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IFeatureRepository _featureRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public TogglePlanFeatureCommandHandler(
        IPlanFeatureRepository planFeatureRepository,
        ISubscriptionPlanRepository planRepository,
        IFeatureRepository featureRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _planFeatureRepository = planFeatureRepository;
        _planRepository = planRepository;
        _featureRepository = featureRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(TogglePlanFeatureCommand request, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure(BillingErrors.PlanNotFound);

        var feature = await _featureRepository.GetByIdAsync(request.FeatureId, cancellationToken);
        if (feature is null)
            return Result.Failure(BillingErrors.FeatureNotFound);

        var existing = await _planFeatureRepository.GetByPlanAndFeatureAsync(
            request.PlanId, request.FeatureId, cancellationToken);

        if (existing is not null)
        {
            existing.Update(request.IsEnabled, request.LimitValue);
            _planFeatureRepository.Update(existing);
        }
        else
        {
            var planFeature = PlanFeature.Create(
                request.PlanId,
                request.FeatureId,
                request.IsEnabled,
                request.LimitValue);

            await _planFeatureRepository.AddAsync(planFeature, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
