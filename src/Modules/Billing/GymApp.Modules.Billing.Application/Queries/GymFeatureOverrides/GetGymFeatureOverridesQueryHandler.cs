using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.GymFeatureOverrides;

internal sealed class GetGymFeatureOverridesQueryHandler : IQueryHandler<GetGymFeatureOverridesQuery, List<GymFeatureOverrideDto>>
{
    private readonly IGymFeatureOverrideRepository _overrideRepository;
    private readonly IFeatureRepository _featureRepository;

    public GetGymFeatureOverridesQueryHandler(
        IGymFeatureOverrideRepository overrideRepository,
        IFeatureRepository featureRepository)
    {
        _overrideRepository = overrideRepository;
        _featureRepository = featureRepository;
    }

    public async Task<Result<List<GymFeatureOverrideDto>>> Handle(GetGymFeatureOverridesQuery request, CancellationToken cancellationToken)
    {
        var overrides = await _overrideRepository.GetByGymIdAsync(request.GymId, cancellationToken);
        var allFeatures = await _featureRepository.GetAllAsync(cancellationToken);
        var featureDict = allFeatures.ToDictionary(f => f.Id);

        var dtos = overrides.Select(o =>
        {
            featureDict.TryGetValue(o.FeatureId, out var feature);

            return new GymFeatureOverrideDto(
                o.Id,
                o.GymId,
                o.FeatureId,
                feature?.Code ?? "Unknown",
                feature?.Name ?? "Unknown",
                o.OverrideValue,
                o.Reason,
                o.SetBy,
                o.SetAt);
        }).ToList();

        return Result.Success(dtos);
    }
}
