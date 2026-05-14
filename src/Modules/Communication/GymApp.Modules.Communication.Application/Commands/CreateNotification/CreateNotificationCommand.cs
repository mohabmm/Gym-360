using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Communication.Application.Commands.CreateNotification;

public sealed record CreateNotificationCommand(
    Guid UserId,
    Guid? GymId,
    string Title,
    string Body,
    string Type,
    Dictionary<string, string>? Data) : ICommand<Guid>;
