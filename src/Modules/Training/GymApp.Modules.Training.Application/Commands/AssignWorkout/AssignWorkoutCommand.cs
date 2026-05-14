using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Training.Application.Commands.AssignWorkout;

public sealed record AssignWorkoutCommand(
    Guid TrainerId,
    Guid ClientId,
    Guid GymId,
    Guid WorkoutId,
    DateTime StartDate,
    DateTime? EndDate,
    string? Notes,
    bool IsRemoteMode) : ICommand<Guid>;
