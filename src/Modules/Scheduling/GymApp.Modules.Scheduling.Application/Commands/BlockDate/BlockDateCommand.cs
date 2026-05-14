using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.BlockDate;

public sealed record BlockDateCommand(
    Guid TrainerId,
    Guid GymId,
    DateOnly Date,
    string? Reason,
    bool AutoNotifyClients) : ICommand;
