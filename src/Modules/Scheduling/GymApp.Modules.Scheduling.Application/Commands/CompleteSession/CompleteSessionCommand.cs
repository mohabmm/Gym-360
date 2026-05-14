using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.CompleteSession;

public sealed record CompleteSessionCommand(Guid SessionId) : ICommand;
