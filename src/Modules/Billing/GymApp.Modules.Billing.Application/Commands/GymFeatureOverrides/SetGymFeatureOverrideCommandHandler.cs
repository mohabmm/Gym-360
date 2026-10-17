using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.GymFeatureOverrides;

internal sealed class SetGymFeatureOverrideCommandHandler : ICommandHandler<SetGymFeatureOverrideCommand>
{
    private readonly IGymFeatureOverrideRepository _overrideRepository;
    private readonly IFeatureRepository _featureRepository;
    private readonly IPlanChangeLogRepository _changeLogRepository;
    private readonly IBillingUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public SetGymFeatureOverrideCommandHandler(
        IGymFeatureOverrideRepository overrideRepository,
        IFeatureRepository featureRepository,
        IPlanChangeLogRepository changeLogRepository,
        IBillingUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _overrideRepository = overrideRepository;
        _featureRepository = featureRepository;
        _changeLogRepository = changeLogRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(SetGymFeatureOverrideCommand request, CancellationToken cancellationToken)
    {
        // Each per-gym billing schema generates its own Feature ids during seed, so the same
        // Code resolves to a different Id per gym. Prefer Code (stable) over Id (per-tenant).
        var feature = !string.IsNullOrWhiteSpace(request.FeatureCode)
            ? await _featureRepository.GetByCodeAsync(request.FeatureCode, cancellationToken)
            : await _featureRepository.GetByIdAsync(request.FeatureId, cancellationToken);
        if (feature is null)
            return Result.Failure(BillingErrors.FeatureNotFound);

        var existing = await _overrideRepository.GetByGymAndFeatureAsync(
            request.GymId, feature.Id, cancellationToken);

        if (existing is not null)
        {
            existing.Update(request.Enabled, request.Reason, request.SetBy);
            _overrideRepository.Update(existing);
        }
        else
        {
            var gymOverride = GymFeatureOverride.Create(new GymFeatureOverrideDetails
            {
                GymId = request.GymId,
                FeatureId = feature.Id,
                OverrideValue = request.Enabled,
                SetBy = request.SetBy,
                Reason = request.Reason
            });

            await _overrideRepository.AddAsync(gymOverride, cancellationToken);
        }

        var log = PlanChangeLog.Create(new PlanChangeLogDetails
        {
            ChangeType = "FeatureOverrideSet",
            ChangedBy = request.SetBy,
            GymId = request.GymId,
            EntityType = "GymFeatureOverride",
            EntityId = feature.Id,
            NewValue = request.Enabled.ToString(),
            Reason = request.Reason
        });

        await _changeLogRepository.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Bust GymPlanService cache so the override takes effect immediately (cache TTL is 5 min).
        await _cacheService.RemoveAsync($"v2:plan-features:{request.GymId}", cancellationToken);

        return Result.Success();
    }
}
