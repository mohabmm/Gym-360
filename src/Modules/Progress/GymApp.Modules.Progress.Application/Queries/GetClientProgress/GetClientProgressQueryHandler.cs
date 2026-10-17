using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Application.Mappers;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Domain.ValueObjects;
using GymApp.Modules.Progress.Application.Queries.Shared;

namespace GymApp.Modules.Progress.Application.Queries.GetClientProgress;

internal sealed class GetClientProgressQueryHandler : IQueryHandler<GetClientProgressQuery, ClientProgressResponse>
{
    private readonly IBodyMetricEntryRepository _metricRepository;
    private readonly IProgressPhotoRepository _photoRepository;
    private readonly IStreakRepository _streakRepository;

    public GetClientProgressQueryHandler(
        IBodyMetricEntryRepository metricRepository,
        IProgressPhotoRepository photoRepository,
        IStreakRepository streakRepository)
    {
        _metricRepository = metricRepository;
        _photoRepository = photoRepository;
        _streakRepository = streakRepository;
    }

    public async Task<Result<ClientProgressResponse>> Handle(
        GetClientProgressQuery request,
        CancellationToken cancellationToken)
    {
        var allMetrics = await _metricRepository.GetByUserAsync(
            request.ClientId, request.GymId, new DateRange(DateOnly.MinValue, DateOnly.FromDateTime(DateTime.UtcNow)), cancellationToken);

        var ordered = allMetrics.OrderBy(m => m.Date).ToList();

        var latest = ordered.LastOrDefault();
        BodyMetricDto? latestDto = latest is not null ? ProgressMapper.ToDto(latest) : null;

        // Take last 10 entries for recent metrics
        var recentMetrics = ordered
            .TakeLast(10)
            .Select(ProgressMapper.ToDto)
            .ToList();

        var changes = BodyMetricChanges.Between(ordered);

        int photoCount = await _photoRepository.GetCountAsync(
            request.ClientId, request.GymId, cancellationToken);

        var workoutStreak = await _streakRepository.GetByUserAndTypeAsync(
            request.ClientId, request.GymId, StreakType.DailyWorkout, cancellationToken);

        var response = new ClientProgressResponse(
            request.ClientId,
            latestDto,
            recentMetrics,
            photoCount,
            workoutStreak?.CurrentStreak ?? 0,
            workoutStreak?.LongestStreak ?? 0,
            changes.WeightChange,
            changes.BodyFatChange);

        return Result.Success(response);
    }
}
