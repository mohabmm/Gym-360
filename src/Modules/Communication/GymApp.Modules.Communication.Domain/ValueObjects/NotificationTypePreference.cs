using System.Text.Json.Serialization;
using GymApp.Modules.Communication.Domain.Enums;

namespace GymApp.Modules.Communication.Domain.ValueObjects;

public sealed class NotificationTypePreference
{
    private NotificationTypePreference() { }

    [JsonConstructor]
    internal NotificationTypePreference(NotificationType notificationType, bool pushEnabled, bool inAppEnabled)
    {
        NotificationType = notificationType;
        PushEnabled = pushEnabled;
        InAppEnabled = inAppEnabled;
    }

    public NotificationType NotificationType { get; private set; }
    public bool PushEnabled { get; private set; }
    public bool InAppEnabled { get; private set; }

    public static NotificationTypePreference Create(
        NotificationType notificationType,
        bool pushEnabled,
        bool inAppEnabled)
    {
        return new NotificationTypePreference
        {
            NotificationType = notificationType,
            PushEnabled = pushEnabled,
            InAppEnabled = inAppEnabled
        };
    }

    public NotificationTypePreference WithPush(bool enabled) =>
        Create(NotificationType, enabled, InAppEnabled);

    public NotificationTypePreference WithInApp(bool enabled) =>
        Create(NotificationType, PushEnabled, enabled);
}
