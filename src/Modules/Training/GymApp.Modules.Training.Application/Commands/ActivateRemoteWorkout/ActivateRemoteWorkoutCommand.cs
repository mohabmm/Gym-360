using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Training.Application.Commands.ActivateRemoteWorkout;

public sealed record ActivateRemoteWorkoutCommand(
    Guid TrainerId,
    Guid ClientId,
    Guid GymId,
    string? Reason) : ICommand;
