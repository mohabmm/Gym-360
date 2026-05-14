using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Communication.Application.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(
    Guid NotificationId,
    Guid UserId) : ICommand;
