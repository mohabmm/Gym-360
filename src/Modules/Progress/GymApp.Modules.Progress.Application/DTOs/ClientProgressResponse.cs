namespace GymApp.Modules.Progress.Application.DTOs;

public sealed record ClientProgressResponse(
    Guid ClientId,
    BodyMetricDto? LatestMetrics,
    List<BodyMetricDto> RecentMetrics,
    int PhotoCount,
    int CurrentWorkoutStreak,
    int LongestWorkoutStreak,
    decimal? WeightChangeFromFirst,
    decimal? BodyFatChangeFromFirst);
