using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Events;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class Announcement : Entity
{
    private Announcement() : base() { }

    private Announcement(
        Guid id,
        Guid gymId,
        string title,
        string content,
        string? imageUrl,
        AnnouncementCategory category,
        TargetAudience targetAudience,
        DateTime? scheduledAt,
        Guid createdBy) : base(id)
    {
        GymId = gymId;
        Title = title;
        Content = content;
        ImageUrl = imageUrl;
        Category = category;
        TargetAudience = targetAudience;
        ScheduledAt = scheduledAt;
        CreatedBy = createdBy;
    }

    public Guid GymId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public string? ImageUrl { get; private set; }
    public AnnouncementCategory Category { get; private set; }
    public TargetAudience TargetAudience { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    public static Announcement Create(
        Guid gymId,
        string title,
        string content,
        string? imageUrl,
        AnnouncementCategory category,
        TargetAudience targetAudience,
        DateTime? scheduledAt,
        Guid createdBy)
    {
        return new Announcement(
            Guid.NewGuid(),
            gymId,
            title,
            content,
            imageUrl,
            category,
            targetAudience,
            scheduledAt,
            createdBy);
    }

    public void Publish()
    {
        PublishedAt = DateTime.UtcNow;
        RaiseDomainEvent(new AnnouncementPublishedDomainEvent(GymId, Id));
    }

    public void Update(string title, string content, string? imageUrl, AnnouncementCategory category, TargetAudience targetAudience)
    {
        Title = title;
        Content = content;
        ImageUrl = imageUrl;
        Category = category;
        TargetAudience = targetAudience;
    }
}
