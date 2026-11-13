using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Commands.PackageInterests;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Identity.Shared;

namespace GymApp.Modules.Billing.Application.Queries.PackageInterests;

internal sealed class GetPackageInterestsQueryHandler
    : IQueryHandler<GetPackageInterestsQuery, IReadOnlyList<PackageInterestDto>>
{
    private readonly IPackageInterestRepository _interests;
    private readonly ISessionPackageRepository _packages;
    private readonly IUserProfileLookup _profiles;
    private readonly IFeatureFlagService _featureFlags;

    public GetPackageInterestsQueryHandler(
        IPackageInterestRepository interests,
        ISessionPackageRepository packages,
        IUserProfileLookup profiles,
        IFeatureFlagService featureFlags)
    {
        _interests = interests;
        _packages = packages;
        _profiles = profiles;
        _featureFlags = featureFlags;
    }

    public async Task<Result<IReadOnlyList<PackageInterestDto>>> Handle(
        GetPackageInterestsQuery request, CancellationToken cancellationToken)
    {
        var gate = await PackageInterestGate.CheckAsync(_featureFlags, request.GymId, cancellationToken);
        if (gate.IsFailure)
            return Result.Failure<IReadOnlyList<PackageInterestDto>>(gate.Error);

        var interests = await _interests.GetByGymAsync(request.GymId, cancellationToken);
        var paidByMember = await PaidDatesByMemberAsync(request.GymId, cancellationToken);

        var rows = interests
            .Select(i => (Interest: i, State: StateOf(i, paidByMember)))
            .Where(r => request.State.Equals(PackageInterestStates.All, StringComparison.OrdinalIgnoreCase)
                || r.State.Equals(request.State, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.State == PackageInterestStates.Pending ? 0 : 1)
            .ThenBy(r => r.Interest.CreatedAt)
            .ToList();

        var profiles = await _profiles.GetProfilesAsync(
            rows.SelectMany(r => new[] { r.Interest.MemberId, r.Interest.TrainerId }).Distinct(), cancellationToken);

        IReadOnlyList<PackageInterestDto> result = rows
            .Select(r => ToDto(r.Interest, r.State, profiles))
            .ToList();
        return Result.Success(result);
    }

    private async Task<IReadOnlyDictionary<Guid, List<DateTime>>> PaidDatesByMemberAsync(
        Guid gymId, CancellationToken cancellationToken)
    {
        var packages = await _packages.GetByGymAsync(gymId, cancellationToken);
        return packages
            .Where(p => !p.IsTrial)
            .GroupBy(p => p.MemberId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.CreatedAt).ToList());
    }

    private static string StateOf(PackageInterest interest, IReadOnlyDictionary<Guid, List<DateTime>> paidByMember)
    {
        var paid = paidByMember.TryGetValue(interest.MemberId, out var dates) ? dates : new List<DateTime>();
        if (interest.IsConvertedBy(paid))
            return PackageInterestStates.Converted;

        return interest.Status == PackageInterestStatus.Dismissed
            ? PackageInterestStates.Dismissed
            : PackageInterestStates.Pending;
    }

    private static PackageInterestDto ToDto(
        PackageInterest interest, string state, IReadOnlyDictionary<Guid, UserProfileInfo> profiles)
    {
        profiles.TryGetValue(interest.MemberId, out var member);
        profiles.TryGetValue(interest.TrainerId, out var trainer);

        return new PackageInterestDto(
            interest.Id,
            interest.MemberId,
            FullName(member),
            member?.PhoneNumber,
            interest.TrainerId,
            FullName(trainer),
            interest.Source.ToString(),
            interest.RecommendedSessions,
            state,
            interest.CreatedAt);
    }

    private static string FullName(UserProfileInfo? profile) =>
        profile is null ? string.Empty : $"{profile.FirstName} {profile.LastName}".Trim();
}
