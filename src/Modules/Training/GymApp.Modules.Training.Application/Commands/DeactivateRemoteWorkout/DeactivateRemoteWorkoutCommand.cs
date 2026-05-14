using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Training.Application.Commands.DeactivateRemoteWorkout;

public sealed record DeactivateRemoteWorkoutCommand(
    Guid TrainerId,
    Guid ClientId,
    Guid GymId) : ICommand;
