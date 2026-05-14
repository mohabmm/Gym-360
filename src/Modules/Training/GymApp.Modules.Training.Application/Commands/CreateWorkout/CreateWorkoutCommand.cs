using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Commands.CreateWorkout;

public sealed record CreateWorkoutCommand(
    Guid TrainerId,
    Guid GymId,
    string Name,
    string? NameAr,
    string? Description,
    List<WorkoutExerciseDto> Exercises,
    int? EstimatedDuration,
    bool IsTemplate) : ICommand<Guid>;
