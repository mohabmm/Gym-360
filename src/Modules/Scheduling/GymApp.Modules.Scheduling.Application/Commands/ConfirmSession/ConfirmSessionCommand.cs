using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.ConfirmSession;

public sealed record ConfirmSessionCommand(Guid SessionId) : ICommand;
