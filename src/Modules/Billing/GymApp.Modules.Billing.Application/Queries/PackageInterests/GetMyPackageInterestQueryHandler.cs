using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Commands.PackageInterests;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Application.Queries.PackageInterests;

internal sealed class GetMyPackageInterestQueryHandler
    : IQueryHandler<GetMyPackageInterestQuery, MyPackageInterestDto>
{
    private readonly IPackageInterestRepository _interests;
    private readonly ISessionPackageRepository _packages;
    private readonly IFeatureFlagService _featureFlags;

    public GetMyPackageInterestQueryHandler(
        IPackageInterestRepository interests,
        ISessionPackageRepository packages,
        IFeatureFlagService featureFlags)
    {
        _interests = interests;
        _packages = packages;
        _featureFlags = featureFlags;
    }

    public async Task<Result<MyPackageInterestDto>> Handle(
        GetMyPackageInterestQuery request, CancellationToken cancellationToken)
    {
        var gate = await PackageInterestGate.CheckAsync(_featureFlags, request.GymId, cancellationToken);
        if (gate.IsFailure)
            return Result.Failure<MyPackageInterestDto>(gate.Error);

        var pending = await PendingPackageInterest.FindAsync(
            _interests, _packages, new PackageOwner(request.GymId, request.MemberId), cancellationToken);

        return Result.Success(pending is null
            ? MyPackageInterestDto.None
            : new MyPackageInterestDto(true, pending.Id, pending.SourcePackageId, pending.CreatedAt));
    }
}
