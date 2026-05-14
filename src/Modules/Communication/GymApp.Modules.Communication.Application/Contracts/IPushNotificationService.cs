namespace GymApp.Modules.Communication.Application.Contracts;

public interface IPushNotificationService
{
    Task SendAsync(
        Guid userId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
