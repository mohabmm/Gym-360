namespace GymApp.Modules.Communication.Application.Contracts;

public interface INotificationService
{
    Task SendNotificationAsync(
        Guid userId,
        string title,
        string body,
        string type,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);

    Task SendPushNotificationAsync(
        Guid userId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
