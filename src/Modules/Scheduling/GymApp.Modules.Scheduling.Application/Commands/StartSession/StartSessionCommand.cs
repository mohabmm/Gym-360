using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.StartSession;

public sealed record StartSessionCommand(Guid SessionId) : ICommand;
