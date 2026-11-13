namespace GymApp.Modules.Progress.Application.DTOs;

/// <summary>
/// pt.progress_measurements: every body-metric entry since day one (oldest first) plus the
/// member's goal. The apps derive the day-one baseline, changes and chart from it.
/// </summary>
public sealed record ProgressJourneyResponse(
    Guid UserId,
    List<BodyMetricDto> Entries,
    ProgressGoalDto? Goal);
