using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.IntegrationEvents;

namespace GymApp.Modules.Billing.Application.Commands.PackageInterests;

internal sealed class ExpressPackageInterestCommandHandler
    : ICommandHandler<ExpressPackageInterestCommand, PackageInterestReceipt>
{
    private readonly ISessionPackageRepository _packages;
    private readonly IPackageInterestRepository _interests;
    private readonly IFeatureFlagService _featureFlags;
    private readonly IIntegrationEventPublisher _integrationEvents;
    private readonly IDateTimeProvider _clock;
    private readonly IBillingUnitOfWork _unitOfWork;

    public ExpressPackageInterestCommandHandler(
        ISessionPackageRepository packages,
        IPackageInterestRepository interests,
        IFeatureFlagService featureFlags,
        IIntegrationEventPublisher integrationEvents,
        IDateTimeProvider clock,
        IBillingUnitOfWork unitOfWork)
    {
        _packages = packages;
        _interests = interests;
        _featureFlags = featureFlags;
        _integrationEvents = integrationEvents;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PackageInterestReceipt>> Handle(
        ExpressPackageInterestCommand request, CancellationToken cancellationToken)
    {
        var gate = await PackageInterestGate.CheckAsync(_featureFlags, request.GymId, cancellationToken);
        if (gate.IsFailure)
            return Result.Failure<PackageInterestReceipt>(gate.Error);

        var package = await _packages.GetByIdAsync(request.SourcePackageId, cancellationToken);
        if (package is null || package.GymId != request.GymId || package.MemberId != request.MemberId)
            return Result.Failure<PackageInterestReceipt>(BillingErrors.PackageNotFound);

        var pending = await FindPendingAsync(new PackageOwner(request.GymId, request.MemberId), cancellationToken);
        if (pending is not null)
            return Result.Success(new PackageInterestReceipt(pending.Id, AlreadyPending: true));

        var interest = PackageInterest.ForPackage(package, _clock.UtcNow);
        await _interests.AddAsync(interest, cancellationToken);
        _integrationEvents.Enqueue(new PackageInterestCreatedIntegrationEvent(
            interest.Id,
            interest.GymId,
            interest.MemberId,
            interest.TrainerId,
            interest.Source.ToString(),
            interest.RecommendedSessions));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new PackageInterestReceipt(interest.Id, AlreadyPending: false));
    }

    private async Task<PackageInterest?> FindPendingAsync(PackageOwner owner, CancellationToken cancellationToken)
    {
        var interests = await _interests.GetByMemberAsync(owner, cancellationToken);
        if (interests.Count == 0)
            return null;

        var paidCreatedAts = await PaidPackageDates.ForMemberAsync(_packages, owner, cancellationToken);
        return interests.FirstOrDefault(i => i.IsPendingGiven(paidCreatedAts));
    }
}

/// <summary>When each of a member's paid packages was created: a lead before one of these is converted.</summary>
internal static class PaidPackageDates
{
    public static async Task<IReadOnlyList<DateTime>> ForMemberAsync(
        ISessionPackageRepository packages, PackageOwner owner, CancellationToken cancellationToken)
    {
        var all = await packages.GetByMemberAsync(owner.GymId, owner.MemberId, cancellationToken);
        return all.Where(p => !p.IsTrial).Select(p => p.CreatedAt).ToList();
    }
}
