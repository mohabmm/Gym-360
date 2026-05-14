using GymApp.Common.Domain;

namespace GymApp.Modules.Gamification.Domain.Entities;

public sealed class Testimonial : Entity
{
    private Testimonial() { }

    private Testimonial(
        Guid id,
        Guid profileId,
        string commentText,
        bool isApproved)
        : base(id)
    {
        ProfileId = profileId;
        CommentText = commentText;
        IsApproved = isApproved;
        ModeratedBy = null;
        ModeratedAt = null;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid ProfileId { get; private set; }
    public string CommentText { get; private set; } = string.Empty;
    public bool IsApproved { get; private set; }
    public Guid? ModeratedBy { get; private set; }
    public DateTime? ModeratedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Testimonial Create(
        Guid profileId,
        string commentText,
        bool isApproved)
    {
        return new Testimonial(
            Guid.NewGuid(),
            profileId,
            commentText,
            isApproved);
    }

    public void Approve(Guid moderatorId)
    {
        IsApproved = true;
        ModeratedBy = moderatorId;
        ModeratedAt = DateTime.UtcNow;
    }

    public void Reject(Guid moderatorId)
    {
        IsApproved = false;
        ModeratedBy = moderatorId;
        ModeratedAt = DateTime.UtcNow;
    }
}
