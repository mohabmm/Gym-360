using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.PackageTemplates;

internal sealed class GetPackageTemplatesQueryHandler : IQueryHandler<GetPackageTemplatesQuery, List<PackageTemplateDto>>
{
    private readonly ISessionPackageTemplateRepository _templateRepository;

    public GetPackageTemplatesQueryHandler(ISessionPackageTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<Result<List<PackageTemplateDto>>> Handle(GetPackageTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _templateRepository.GetByGymIdAsync(request.GymId, cancellationToken);

        var dtos = templates.Select(t => new PackageTemplateDto(
            t.Id,
            t.GymId,
            t.Name,
            t.SessionCount,
            t.PricePerSession.Amount,
            t.TotalPrice.Amount,
            t.PricePerSession.Currency,
            t.IsActive,
            t.CreatedAt)).ToList();

        return Result.Success(dtos);
    }
}
