using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.UnblockDate;

public sealed record UnblockDateCommand(
    Guid TrainerId,
    Guid GymId,
    DateOnly Date) : ICommand;
