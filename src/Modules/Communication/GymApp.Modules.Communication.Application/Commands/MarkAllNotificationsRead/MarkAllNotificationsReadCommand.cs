using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Communication.Application.Commands.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadCommand(Guid UserId) : ICommand;
