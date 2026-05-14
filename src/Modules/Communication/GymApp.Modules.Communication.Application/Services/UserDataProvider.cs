using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Communication.Domain.Repositories;

namespace GymApp.Modules.Communication.Application.Services;

internal sealed class UserDataProvider : IUserDataProvider
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationPreferenceRepository _preferenceRepository;

    public UserDataProvider(
        INotificationRepository notificationRepository,
        INotificationPreferenceRepository preferenceRepository)
    {
        _notificationRepository = notificationRepository;
        _preferenceRepository = preferenceRepository;
    }

    public string ModuleName => "Communication";

    public async Task<object> GetUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Return notification preferences and notification metadata (not message content for privacy)
        var preferences = await _preferenceRepository.GetByUserAsync(userId, gymId: null, cancellationToken);
        var recentNotifications = await _notificationRepository.GetByUserAsync(
            userId, gymId: null, page: 1, pageSize: 100, cancellationToken);

        return new
        {
            NotificationPreferences = preferences is null ? null : new
            {
                preferences.UserId,
                preferences.GymId,
                preferences.QuietHoursStart,
                preferences.QuietHoursEnd,
                Preferences = preferences.Preferences.Select(p => new
                {
                    p.NotificationType,
                    p.PushEnabled,
                    p.InAppEnabled
                })
            },
            Notifications = recentNotifications.Select(n => new
            {
                n.Id,
                n.Type,
                n.Title,
                n.IsRead,
                n.CreatedAt,
                n.GymId
            }),
            Note = "Conversation message content is not included in the export to protect other participants' privacy."
        };
    }
}
