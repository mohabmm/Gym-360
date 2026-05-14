using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Domain.Repositories;

namespace GymApp.Modules.Progress.Application.Queries.GetBodyMetricTrends;

internal sealed class GetBodyMetricTrendsQueryHandler : IQueryHandler<GetBodyMetricTrendsQuery, MetricTrendsResponse>
{
    private readonly IBodyMetricEntryRepository _repository;

    public GetBodyMetricTrendsQueryHandler(IBodyMetricEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<MetricTrendsResponse>> Handle(GetBodyMetricTrendsQuery request, CancellationToken cancellationToken)
    {
        var entries = await _repository.GetByUserAsync(
            request.UserId, request.GymId, request.From, request.To, cancellationToken);

        var ordered = entries.OrderBy(e => e.Date).ToList();

        var labels = ordered.Select(e => e.Date.ToString("yyyy-MM-dd")).ToList();
        var weightValues = ordered.Select(e => e.Weight).ToList();
        var bodyFatValues = ordered.Select(e => e.BodyFatPercentage).ToList();
        var waistValues = ordered.Select(e => e.Measurements?.Waist).ToList();
        var chestValues = ordered.Select(e => e.Measurements?.Chest).ToList();

        var response = new MetricTrendsResponse(
            labels,
            weightValues,
            bodyFatValues,
            waistValues,
            chestValues);

        return Result.Success(response);
    }
}
