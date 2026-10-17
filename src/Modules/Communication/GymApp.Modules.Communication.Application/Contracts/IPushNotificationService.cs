namespace GymApp.Modules.Communication.Application.Contracts;

public interface IPushNotificationService
{
    Task SendAsync(
        Guid userId,
        PushContent content,
        CancellationToken cancellationToken = default);
}
