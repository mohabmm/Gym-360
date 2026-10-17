using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Events;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class Announcement : Entity
{
    private Announcement() : base() { }

    private Announcement(Guid id) : base(id) { }

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
        AnnouncementDetails details,
        DateTime? scheduledAt,
        Guid createdBy)
    {
        return new Announcement(Guid.NewGuid())
        {
            GymId = gymId,
            Title = details.Title,
            Content = details.Content,
            ImageUrl = details.ImageUrl,
            Category = details.Category,
            TargetAudience = details.TargetAudience,
            ScheduledAt = scheduledAt,
            CreatedBy = createdBy
        };
    }

    public void Publish()
    {
        PublishedAt = DateTime.UtcNow;
        RaiseDomainEvent(new AnnouncementPublishedDomainEvent(GymId, Id));
    }

    public void Unpublish()
    {
        PublishedAt = null;
    }

    public void Update(AnnouncementDetails details)
    {
        Title = details.Title;
        Content = details.Content;
        ImageUrl = details.ImageUrl;
        Category = details.Category;
        TargetAudience = details.TargetAudience;
    }
}
