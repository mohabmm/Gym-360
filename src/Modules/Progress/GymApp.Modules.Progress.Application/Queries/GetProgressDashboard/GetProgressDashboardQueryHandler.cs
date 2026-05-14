using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Application.Mappers;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.Repositories;

namespace GymApp.Modules.Progress.Application.Queries.GetProgressDashboard;

internal sealed class GetProgressDashboardQueryHandler : IQueryHandler<GetProgressDashboardQuery, ProgressDashboardResponse>
{
    private readonly IBodyMetricEntryRepository _metricRepository;
    private readonly IProgressPhotoRepository _photoRepository;
    private readonly IStreakRepository _streakRepository;

    public GetProgressDashboardQueryHandler(
        IBodyMetricEntryRepository metricRepository,
        IProgressPhotoRepository photoRepository,
        IStreakRepository streakRepository)
    {
        _metricRepository = metricRepository;
        _photoRepository = photoRepository;
        _streakRepository = streakRepository;
    }

    public async Task<Result<ProgressDashboardResponse>> Handle(
        GetProgressDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var latest = await _metricRepository.GetLatestAsync(
            request.UserId, request.GymId, cancellationToken);

        BodyMetricDto? latestDto = latest is not null ? ProgressMapper.ToDto(latest) : null;

        // Get all metrics to compute weight/body fat change from first entry
        var allMetrics = await _metricRepository.GetByUserAsync(
            request.UserId, request.GymId, DateOnly.MinValue, DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);

        decimal? weightChange = null;
        decimal? bodyFatChange = null;

        if (allMetrics.Count >= 2)
        {
            var ordered = allMetrics.OrderBy(m => m.Date).ToList();
            var first = ordered.First();
            var last = ordered.Last();

            if (first.Weight.HasValue && last.Weight.HasValue)
                weightChange = last.Weight.Value - first.Weight.Value;

            if (first.BodyFatPercentage.HasValue && last.BodyFatPercentage.HasValue)
                bodyFatChange = last.BodyFatPercentage.Value - first.BodyFatPercentage.Value;
        }

        int photoCount = await _photoRepository.GetCountAsync(
            request.UserId, request.GymId, cancellationToken);

        var workoutStreak = await _streakRepository.GetByUserAndTypeAsync(
            request.UserId, request.GymId, StreakType.DailyWorkout, cancellationToken);

        var checkInStreak = await _streakRepository.GetByUserAndTypeAsync(
            request.UserId, request.GymId, StreakType.DailyCheckIn, cancellationToken);

        var response = new ProgressDashboardResponse(
            latestDto,
            weightChange,
            bodyFatChange,
            photoCount,
            workoutStreak?.CurrentStreak ?? 0,
            workoutStreak?.LongestStreak ?? 0,
            checkInStreak?.CurrentStreak ?? 0,
            checkInStreak?.LongestStreak ?? 0);

        return Result.Success(response);
    }
}
