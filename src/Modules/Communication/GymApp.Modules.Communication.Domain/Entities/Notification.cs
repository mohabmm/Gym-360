using GymApp.Common.Domain;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Events;

namespace GymApp.Modules.Communication.Domain.Entities;

public sealed class Notification : Entity
{
    private readonly Dictionary<string, string> _data = new();

    private Notification() { }

    public Guid UserId { get; private set; }
    public Guid? GymId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public NotificationType Type { get; private set; }
    public IReadOnlyDictionary<string, string> Data => _data;
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public bool IsPushSent { get; private set; }
    public DateTime? PushSentAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Notification Create(
        Guid userId,
        string title,
        string body,
        NotificationType type,
        Guid? gymId = null,
        Dictionary<string, string>? data = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GymId = gymId,
            Title = title,
            Body = body,
            Type = type,
            IsRead = false,
            IsPushSent = false,
            CreatedAt = DateTime.UtcNow
        };

        if (data is not null)
        {
            foreach (var kvp in data)
            {
                notification._data[kvp.Key] = kvp.Value;
            }
        }

        notification.RaiseDomainEvent(
            new NotificationCreatedDomainEvent(notification.Id, userId, type.ToString()));

        return notification;
    }

    public void MarkAsRead()
    {
        if (IsRead) return;

        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }

    public void MarkPushSent()
    {
        IsPushSent = true;
        PushSentAt = DateTime.UtcNow;
    }
}
