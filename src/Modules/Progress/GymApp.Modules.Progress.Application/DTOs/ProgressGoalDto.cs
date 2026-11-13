namespace GymApp.Modules.Progress.Application.DTOs;

public sealed record ProgressGoalDto(
    string Goal,
    decimal? TargetWeight,
    Guid SetBy,
    DateTime UpdatedAt);
