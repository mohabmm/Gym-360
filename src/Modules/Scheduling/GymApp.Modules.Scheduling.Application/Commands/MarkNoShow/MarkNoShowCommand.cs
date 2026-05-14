using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.MarkNoShow;

public sealed record MarkNoShowCommand(Guid SessionId) : ICommand;
