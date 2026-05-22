using GymApp.Modules.Gyms.Shared;
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

    public GetMyPackagesQueryHandler(
        ISessionPackageRepository packageRepository,
        IGymMembershipChecker membershipChecker)
    {
        _packageRepository = packageRepository;
        _membershipChecker = membershipChecker;
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

        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new MySessionPackageDto(
                p.Id,
                p.Name,
                p.TrainerId,
                // Cross-module trainer display-name lookup does not yet exist.
                // The mobile client resolves names via user endpoints; returning
                // null keeps this endpoint self-contained and module-bounded.
                TrainerName: null,
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
