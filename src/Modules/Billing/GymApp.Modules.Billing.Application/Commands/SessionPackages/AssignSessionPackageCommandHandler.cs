using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.SessionPackages;

internal sealed class AssignSessionPackageCommandHandler : ICommandHandler<AssignSessionPackageCommand, Guid>
{
    private readonly ISessionPackageRepository _packageRepository;
    private readonly ISessionPackageTemplateRepository _templateRepository;
    private readonly IFeatureFlagService _featureFlags;
    private readonly IBillingUnitOfWork _unitOfWork;

    public AssignSessionPackageCommandHandler(
        ISessionPackageRepository packageRepository,
        ISessionPackageTemplateRepository templateRepository,
        IFeatureFlagService featureFlags,
        IBillingUnitOfWork unitOfWork)
    {
        _packageRepository = packageRepository;
        _templateRepository = templateRepository;
        _featureFlags = featureFlags;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AssignSessionPackageCommand request, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(request.GymId, FeatureKeys.PtPackages, cancellationToken))
            return Result.Failure<Guid>(FeatureFlagErrors.FeatureDisabled(FeatureKeys.PtPackages));

        if (await HasOtherCoachAsync(request, cancellationToken))
            return Result.Failure<Guid>(BillingErrors.CoachAlreadyAssigned);

        // While pt.auto_expiry is off (default) packages never expire, so none gets an expiry date.
        var expiresAt = await _featureFlags.IsEnabledAsync(request.GymId, FeatureKeys.PtAutoExpiry, cancellationToken)
            ? request.ExpiresAt
            : null;

        var template = await _templateRepository.GetByIdAsync(request.TemplateId, cancellationToken);

        if (template is null)
        {
            return Result.Failure<Guid>(BillingErrors.TemplateNotFound);
        }

        var package = SessionPackage.Create(new SessionPackageDetails
        {
            GymId = request.GymId,
            TrainerId = request.TrainerId,
            MemberId = request.MemberId,
            TemplateId = request.TemplateId,
            Name = template.Name,
            TotalSessions = template.SessionCount,
            PricePerSession = template.PricePerSession,
            TotalPrice = template.TotalPrice,
            AssignedBy = request.AssignedBy,
            ExpiresAt = expiresAt
        });

        await _packageRepository.AddAsync(package, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(package.Id);
    }

    /// <summary>
    /// One coach at a time: while the member has sessions left, a new package must be with the
    /// same coach (a renewal). A package with a different coach waits until those sessions are used.
    /// </summary>
    private async Task<bool> HasOtherCoachAsync(AssignSessionPackageCommand request, CancellationToken cancellationToken)
    {
        if (!await _packageRepository.HasActivePackageAsync(
                new PackageOwner(request.GymId, request.MemberId), DateTime.UtcNow, cancellationToken))
            return false;

        var withSameCoach = await _packageRepository.GetActiveByMemberAndTrainerAsync(
            new PackageParticipants(request.GymId, request.MemberId, request.TrainerId), cancellationToken);
        return withSameCoach is null;
    }
}
