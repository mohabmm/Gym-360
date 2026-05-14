using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Commands.UpdateWorkout;

public sealed record UpdateWorkoutCommand(
    Guid WorkoutId,
    string? Name,
    string? NameAr,
    string? Description,
    List<WorkoutExerciseDto>? Exercises,
    int? EstimatedDuration) : ICommand;
