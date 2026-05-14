using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.GymFeatureOverrides;

internal sealed class RemoveGymFeatureOverrideCommandHandler : ICommandHandler<RemoveGymFeatureOverrideCommand>
{
    private readonly IGymFeatureOverrideRepository _overrideRepository;
    private readonly IBillingUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public RemoveGymFeatureOverrideCommandHandler(
        IGymFeatureOverrideRepository overrideRepository,
        IBillingUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _overrideRepository = overrideRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(RemoveGymFeatureOverrideCommand request, CancellationToken cancellationToken)
    {
        var existing = await _overrideRepository.GetByGymAndFeatureAsync(
            request.GymId, request.FeatureId, cancellationToken);

        if (existing is null)
        {
            return Result.Success();
        }

        _overrideRepository.Delete(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Bust GymPlanService cache so the override removal takes effect immediately.
        await _cacheService.RemoveAsync($"v2:plan-features:{request.GymId}", cancellationToken);

        return Result.Success();
    }
}
