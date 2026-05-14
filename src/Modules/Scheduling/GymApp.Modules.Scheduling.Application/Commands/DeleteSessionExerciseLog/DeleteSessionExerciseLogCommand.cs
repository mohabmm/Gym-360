using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.DeleteSessionExerciseLog;

public sealed record DeleteSessionExerciseLogCommand(
    Guid SessionId,
    Guid LogId,
    Guid TrainerId) : ICommand;
