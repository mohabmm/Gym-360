using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Commands.LogWorkout;

public sealed record LogWorkoutCommand(
    Guid UserId,
    Guid GymId,
    Guid? WorkoutId,
    DateOnly Date,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int? DurationMinutes,
    string? Notes,
    List<LogSetDto> Sets,
    int? PerceivedExertion) : ICommand<Guid>;
