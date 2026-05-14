using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.SessionPackages;

internal sealed class GetMyClientPackagesQueryHandler : IQueryHandler<GetMyClientPackagesQuery, List<SessionPackageDto>>
{
    private readonly ISessionPackageRepository _packageRepository;

    public GetMyClientPackagesQueryHandler(ISessionPackageRepository packageRepository)
    {
        _packageRepository = packageRepository;
    }

    public async Task<Result<List<SessionPackageDto>>> Handle(GetMyClientPackagesQuery request, CancellationToken cancellationToken)
    {
        var packages = await _packageRepository.GetByTrainerAsync(
            request.GymId, request.TrainerId, cancellationToken);

        var dtos = packages.Select(p => new SessionPackageDto(
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
            p.CreatedAt)).ToList();

        return Result.Success(dtos);
    }
}
