using GymApp.Common.Domain;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Domain.Entities;

public sealed class NotificationPreference : Entity
{
    private readonly List<NotificationTypePreference> _preferences = new();

    private NotificationPreference() { }

    public Guid UserId { get; private set; }
    public Guid? GymId { get; private set; }
    public IReadOnlyList<NotificationTypePreference> Preferences => _preferences.AsReadOnly();
    public TimeOnly? QuietHoursStart { get; private set; }
    public TimeOnly? QuietHoursEnd { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static NotificationPreference Create(Guid userId, Guid? gymId = null)
    {
        var preference = new NotificationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GymId = gymId,
            UpdatedAt = DateTime.UtcNow
        };

        // Initialize all notification types as enabled by default
        foreach (NotificationType type in Enum.GetValues<NotificationType>())
        {
            preference._preferences.Add(
                NotificationTypePreference.Create(type, pushEnabled: true, inAppEnabled: true));
        }

        return preference;
    }

    public bool IsEnabled(NotificationType notificationType)
    {
        var pref = _preferences.FirstOrDefault(p => p.NotificationType == notificationType);
        return pref is null || pref.PushEnabled || pref.InAppEnabled;
    }

    public bool IsPushEnabled(NotificationType notificationType)
    {
        var pref = _preferences.FirstOrDefault(p => p.NotificationType == notificationType);
        return pref is null || pref.PushEnabled;
    }

    public bool IsInAppEnabled(NotificationType notificationType)
    {
        var pref = _preferences.FirstOrDefault(p => p.NotificationType == notificationType);
        return pref is null || pref.InAppEnabled;
    }

    public void SetPreference(NotificationType type, bool pushEnabled, bool inAppEnabled)
    {
        var existing = _preferences.FirstOrDefault(p => p.NotificationType == type);
        if (existing is not null)
        {
            _preferences.Remove(existing);
        }

        _preferences.Add(NotificationTypePreference.Create(type, pushEnabled, inAppEnabled));
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetQuietHours(TimeOnly? start, TimeOnly? end)
    {
        QuietHoursStart = start;
        QuietHoursEnd = end;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsInQuietHours(TimeOnly currentTime)
    {
        if (QuietHoursStart is null || QuietHoursEnd is null)
            return false;

        var start = QuietHoursStart.Value;
        var end = QuietHoursEnd.Value;

        // Handle overnight quiet hours (e.g., 22:00 to 07:00)
        if (start > end)
            return currentTime >= start || currentTime <= end;

        return currentTime >= start && currentTime <= end;
    }
}
