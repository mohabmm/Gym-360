using GymApp.Modules.Gyms.Shared;
using GymApp.Modules.Identity.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.SessionPackages;

internal sealed class GetMyPackagesQueryHandler
    : IQueryHandler<GetMyPackagesQuery, PagedResponse<MySessionPackageDto>>
{
    private const int MaxPageSize = 50;

    private readonly ISessionPackageRepository _packageRepository;
    private readonly IGymMembershipChecker _membershipChecker;
    private readonly IUserDisplayLookup _userDisplayLookup;

    public GetMyPackagesQueryHandler(
        ISessionPackageRepository packageRepository,
        IGymMembershipChecker membershipChecker,
        IUserDisplayLookup userDisplayLookup)
    {
        _packageRepository = packageRepository;
        _membershipChecker = membershipChecker;
        _userDisplayLookup = userDisplayLookup;
    }

    public async Task<Result<PagedResponse<MySessionPackageDto>>> Handle(
        GetMyPackagesQuery request,
        CancellationToken cancellationToken)
    {
        if (await _membershipChecker.IsGymSuspendedAsync(request.GymId, cancellationToken))
            return Result.Failure<PagedResponse<MySessionPackageDto>>(GymAccessErrors.GymSuspended);

        // The endpoint is gated to the trainee role (RequireAuthorization("trainee")),
        // so a caller reaching this handler is always a trainee. A failed membership
        // check therefore means "this trainee has no relationship to this gym" —
        // NotAMemberOfGym, not MemberRoleRequired (which is for wrong-role callers).
        if (!await _membershipChecker.IsActiveMemberAsync(request.GymId, request.UserId, cancellationToken))
            return Result.Failure<PagedResponse<MySessionPackageDto>>(GymAccessErrors.NotAMemberOfGym);

        var window = PageWindow.Create(request.Page, request.PageSize, MaxPageSize);

        var all = await _packageRepository.GetByMemberAsync(
            request.GymId, request.UserId, cancellationToken);

        var ordered = OrderForDisplay(FilterByStatus(all, request.Status));
        var pagePackages = window.Slice(ordered);
        var trainerNames = await ResolveTrainerNamesAsync(pagePackages, cancellationToken);

        var items = pagePackages
            .Select(p => ToDto(p, trainerNames))
            .ToList();

        return Result.Success(window.ToResponse(items, ordered.Count));
    }

    private static IEnumerable<SessionPackage> FilterByStatus(
        IReadOnlyList<SessionPackage> all,
        string status)
    {
        if (status == "All")
            return all;

        var wanted = ParseStatusFilter(status);
        return all.Where(p => p.Status == wanted);
    }

    // Unknown values fall back to Active (the endpoint default).
    private static PackageStatus ParseStatusFilter(string status) => status switch
    {
        "Exhausted" => PackageStatus.Exhausted,
        "Expired" => PackageStatus.Expired,
        _ => PackageStatus.Active
    };

    // Ordering per PRD: Active first, then ExpiresAt asc (nulls last), then CreatedAt desc.
    private static List<SessionPackage> OrderForDisplay(IEnumerable<SessionPackage> packages) =>
        packages
            .OrderBy(p => p.Status == PackageStatus.Active ? 0 : 1)
            .ThenBy(p => p.ExpiresAt ?? DateTime.MaxValue)
            .ThenByDescending(p => p.CreatedAt)
            .ToList();

    // Resolve trainer display names cross-module via the Identity .Shared
    // contract (Rule 3-compliant). Batched for the page slice only.
    private async Task<IReadOnlyDictionary<Guid, UserDisplay>?> ResolveTrainerNamesAsync(
        List<SessionPackage> pagePackages,
        CancellationToken cancellationToken)
    {
        var trainerIds = pagePackages
            .Select(p => p.TrainerId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        return trainerIds.Count == 0
            ? null
            : await _userDisplayLookup.GetDisplayNamesAsync(trainerIds, cancellationToken);
    }

    private static MySessionPackageDto ToDto(
        SessionPackage p,
        IReadOnlyDictionary<Guid, UserDisplay>? trainerNames) =>
        new(
            p.Id,
            p.Name,
            p.TrainerId,
            TrainerName: TrainerNameOf(p, trainerNames),
            p.TotalSessions,
            p.RemainingSessions,
            p.Status.ToString(),
            new MoneyDto(p.PricePerSession.Amount, p.PricePerSession.Currency),
            new MoneyDto(p.TotalPrice.Amount, p.TotalPrice.Currency),
            // SessionPackage does not track AssignedAt separately from
            // CreatedAt — assignment IS creation in this aggregate.
            AssignedAt: p.CreatedAt,
            p.ExpiresAt,
            LastUsedAt: p.UsageHistory
                .OrderByDescending(u => u.UsedAt)
                .Select(u => (DateTime?)u.UsedAt)
                .FirstOrDefault());

    private static string? TrainerNameOf(
        SessionPackage p,
        IReadOnlyDictionary<Guid, UserDisplay>? trainerNames) =>
        trainerNames is not null && trainerNames.TryGetValue(p.TrainerId, out var display)
            ? display.FullName
            : null;
}
