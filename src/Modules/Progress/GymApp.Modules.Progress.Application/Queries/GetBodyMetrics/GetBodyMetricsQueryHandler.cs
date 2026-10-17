using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Application.Mappers;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Modules.Progress.Application.Queries.GetBodyMetrics;

internal sealed class GetBodyMetricsQueryHandler : IQueryHandler<GetBodyMetricsQuery, List<BodyMetricDto>>
{
    private readonly IBodyMetricEntryRepository _repository;

    public GetBodyMetricsQueryHandler(IBodyMetricEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<BodyMetricDto>>> Handle(GetBodyMetricsQuery request, CancellationToken cancellationToken)
    {
        var from = request.From ?? DateOnly.MinValue;
        var to = request.To ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var entries = await _repository.GetByUserAsync(
            request.UserId, request.GymId, new DateRange(from, to), cancellationToken);

        var dtos = entries.Select(ProgressMapper.ToDto).ToList();

        return Result.Success(dtos);
    }
}
