namespace GymApp.Modules.Progress.Application.DTOs;

public sealed record ProgressDashboardResponse(
    BodyMetricDto? LatestMetrics,
    decimal? WeightChangeFromFirst,
    decimal? BodyFatChangeFromFirst,
    int PhotoCount,
    int CurrentWorkoutStreak,
    int LongestWorkoutStreak,
    int CurrentCheckInStreak,
    int LongestCheckInStreak);
