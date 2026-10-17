using GymApp.Common.Domain;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Events;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Domain.Entities;

public sealed class Notification : Entity
{
    private readonly Dictionary<string, string> _data = new();

    // Reserved Data keys holding the English text of a localized notification.
    // Title/Body carry the Arabic (primary) text; see CreateLocalized.
    public const string TitleEnKey = "_titleEn";
    public const string BodyEnKey = "_bodyEn";

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

    public static Notification Create(NotificationDetails details, string title, string body)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = details.UserId,
            GymId = details.GymId,
            Title = title,
            Body = body,
            Type = details.Type,
            IsRead = false,
            IsPushSent = false,
            CreatedAt = DateTime.UtcNow
        };

        notification.CopyData(details.Data);

        notification.RaiseDomainEvent(
            new NotificationCreatedDomainEvent(notification.Id, details.UserId, details.Type.ToString()));

        return notification;
    }

    /// <summary>
    /// Creates an Arabic-first notification: Arabic is stored as the primary
    /// Title/Body (what push and Arabic clients show), English rides along in
    /// <see cref="Data"/> so English-locale clients can be served via <see cref="TextFor"/>.
    /// </summary>
    public static Notification CreateLocalized(NotificationDetails details, NotificationText text)
    {
        var notification = Create(details, text.TitleAr, text.BodyAr);
        notification._data[TitleEnKey] = text.TitleEn;
        notification._data[BodyEnKey] = text.BodyEn;
        return notification;
    }

    /// <summary>
    /// Title/body for a client language tag (e.g. "en", "en-US", "ar"). English is
    /// returned only when requested and available; everything else gets the primary text.
    /// </summary>
    public (string Title, string Body) TextFor(string? language)
    {
        var wantsEnglish = language?.TrimStart().StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;
        if (!wantsEnglish)
            return (Title, Body);

        return _data.TryGetValue(TitleEnKey, out var titleEn) && _data.TryGetValue(BodyEnKey, out var bodyEn)
            ? (titleEn, bodyEn)
            : (Title, Body);
    }

    /// <summary>Client-facing metadata: <see cref="Data"/> without the reserved text keys.</summary>
    public Dictionary<string, string> PublicData() =>
        _data.Where(kvp => kvp.Key != TitleEnKey && kvp.Key != BodyEnKey)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    private void CopyData(Dictionary<string, string>? data)
    {
        if (data is null)
            return;

        foreach (var kvp in data)
        {
            _data[kvp.Key] = kvp.Value;
        }
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
