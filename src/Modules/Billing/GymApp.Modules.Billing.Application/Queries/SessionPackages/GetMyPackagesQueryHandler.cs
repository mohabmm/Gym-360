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
    private const int MinPageSize = 1;
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

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, MinPageSize, MaxPageSize);

        var all = await _packageRepository.GetByMemberAsync(
            request.GymId, request.UserId, cancellationToken);

        IEnumerable<SessionPackage> filtered = request.Status switch
        {
            "All" => all,
            "Active" => all.Where(p => p.Status == PackageStatus.Active),
            "Exhausted" => all.Where(p => p.Status == PackageStatus.Exhausted),
            "Expired" => all.Where(p => p.Status == PackageStatus.Expired),
            _ => all.Where(p => p.Status == PackageStatus.Active)
        };

        // Ordering per PRD: Active first, then ExpiresAt asc (nulls last), then CreatedAt desc.
        var ordered = filtered
            .OrderBy(p => p.Status == PackageStatus.Active ? 0 : 1)
            .ThenBy(p => p.ExpiresAt ?? DateTime.MaxValue)
            .ThenByDescending(p => p.CreatedAt)
            .ToList();

        var totalCount = ordered.Count;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        var pagePackages = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Resolve trainer display names cross-module via the Identity .Shared
        // contract (Rule 3-compliant). Batched for the page slice only.
        var trainerIds = pagePackages
            .Select(p => p.TrainerId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var trainerNames = trainerIds.Count == 0
            ? null
            : await _userDisplayLookup.GetDisplayNamesAsync(trainerIds, cancellationToken);

        var items = pagePackages
            .Select(p => new MySessionPackageDto(
                p.Id,
                p.Name,
                p.TrainerId,
                TrainerName: trainerNames is not null
                    && trainerNames.TryGetValue(p.TrainerId, out var display)
                        ? display.FullName
                        : null,
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
                    .FirstOrDefault()))
            .ToList();

        var response = new PagedResponse<MySessionPackageDto>(items, page, pageSize, totalCount, totalPages);
        return Result.Success(response);
    }
}
