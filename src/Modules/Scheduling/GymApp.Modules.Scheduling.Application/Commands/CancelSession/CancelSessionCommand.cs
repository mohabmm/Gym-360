using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.CancelSession;

public sealed record CancelSessionCommand(
    Guid SessionId,
    Guid CancelledBy,
    string? Reason) : ICommand;
