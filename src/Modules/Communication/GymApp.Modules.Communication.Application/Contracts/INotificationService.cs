namespace GymApp.Modules.Communication.Application.Contracts;

public interface INotificationService
{
    Task SendNotificationAsync(
        Guid userId,
        PushContent content,
        string type,
        CancellationToken cancellationToken = default);

    Task SendPushNotificationAsync(
        Guid userId,
        PushContent content,
        CancellationToken cancellationToken = default);
}
