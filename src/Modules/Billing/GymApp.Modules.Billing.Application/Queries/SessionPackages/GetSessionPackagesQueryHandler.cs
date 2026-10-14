using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.SessionPackages;

internal sealed class GetSessionPackagesQueryHandler : IQueryHandler<GetSessionPackagesQuery, List<SessionPackageDto>>
{
    private readonly ISessionPackageRepository _packageRepository;

    public GetSessionPackagesQueryHandler(ISessionPackageRepository packageRepository)
    {
        _packageRepository = packageRepository;
    }

    public async Task<Result<List<SessionPackageDto>>> Handle(GetSessionPackagesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<SessionPackage> packages;

        if (request.MemberId.HasValue)
        {
            packages = await _packageRepository.GetByMemberAsync(
                request.GymId, request.MemberId.Value, cancellationToken);
        }
        else if (request.TrainerId.HasValue)
        {
            packages = await _packageRepository.GetByTrainerAsync(
                request.GymId, request.TrainerId.Value, cancellationToken);
        }
        else
        {
            // No member/trainer filter: the staff "Active Packages" view lists
            // every package in the gym.
            packages = await _packageRepository.GetByGymAsync(
                request.GymId, cancellationToken);
        }

        var filteredPackages = packages.AsEnumerable();

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<PackageStatus>(request.Status, true, out var status))
        {
            filteredPackages = filteredPackages.Where(p => p.Status == status);
        }

        var dtos = filteredPackages.Select(MapToDto).ToList();

        return Result.Success(dtos);
    }

    private static SessionPackageDto MapToDto(SessionPackage p) => new(
        p.Id,
        p.GymId,
        p.TrainerId,
        p.MemberId,
        p.TemplateId,
        p.Name,
        p.TotalSessions,
        p.RemainingSessions,
        p.PricePerSession.Amount,
        p.TotalPrice.Amount,
        p.PricePerSession.Currency,
        p.Status.ToString(),
        p.ExpiresAt,
        p.UsageHistory.Select(u => new PackageUsageDto(
            u.Id,
            u.SessionId,
            u.UsedAt,
            u.UsageType.ToString())).ToList(),
        p.AssignedBy,
        p.CreatedAt);
}
