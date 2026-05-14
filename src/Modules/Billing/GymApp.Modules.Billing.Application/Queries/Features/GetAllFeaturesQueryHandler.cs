using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.Features;

internal sealed class GetAllFeaturesQueryHandler : IQueryHandler<GetAllFeaturesQuery, List<FeatureDto>>
{
    private readonly IFeatureRepository _featureRepository;

    public GetAllFeaturesQueryHandler(IFeatureRepository featureRepository)
    {
        _featureRepository = featureRepository;
    }

    public async Task<Result<List<FeatureDto>>> Handle(GetAllFeaturesQuery request, CancellationToken cancellationToken)
    {
        var features = await _featureRepository.GetAllAsync(cancellationToken);

        var dtos = features
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name)
            .Select(f => new FeatureDto(f.Id, f.Code, f.Name, f.Description, f.Category, f.IsActive, f.SortOrder))
            .ToList();

        return Result.Success(dtos);
    }
}
