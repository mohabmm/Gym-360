namespace GymApp.Modules.Training.Application.DTOs;

public sealed record WorkoutLogDto(
    Guid Id,
    Guid UserId,
    Guid GymId,
    Guid? WorkoutId,
    DateOnly Date,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int? DurationMinutes,
    string? Notes,
    List<LogSetDto> Sets,
    List<string> MuscleGroupsWorked,
    int? PerceivedExertion);
