using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Commands.PackageInterests;

internal sealed class DismissPackageInterestCommandHandler : ICommandHandler<DismissPackageInterestCommand>
{
    private readonly IPackageInterestRepository _interests;
    private readonly IFeatureFlagService _featureFlags;
    private readonly IDateTimeProvider _clock;
    private readonly IBillingUnitOfWork _unitOfWork;

    public DismissPackageInterestCommandHandler(
        IPackageInterestRepository interests,
        IFeatureFlagService featureFlags,
        IDateTimeProvider clock,
        IBillingUnitOfWork unitOfWork)
    {
        _interests = interests;
        _featureFlags = featureFlags;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DismissPackageInterestCommand request, CancellationToken cancellationToken)
    {
        var gate = await PackageInterestGate.CheckAsync(_featureFlags, request.GymId, cancellationToken);
        if (gate.IsFailure)
            return gate;

        var interest = await _interests.GetByIdAsync(request.InterestId, cancellationToken);
        if (interest is null || interest.GymId != request.GymId)
            return Result.Failure(BillingErrors.PackageInterestNotFound);

        var dismissed = interest.Dismiss(request.StaffUserId, _clock.UtcNow);
        if (dismissed.IsFailure)
            return dismissed;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
