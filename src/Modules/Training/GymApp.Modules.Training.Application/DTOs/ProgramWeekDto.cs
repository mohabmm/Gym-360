namespace GymApp.Modules.Training.Application.DTOs;

public sealed record ProgramWeekDto(
    int WeekNumber,
    string? Theme,
    List<ProgramDayWorkoutDto> DayWorkouts);

public sealed record ProgramDayWorkoutDto(
    string DayOfWeek,
    Guid WorkoutId);
