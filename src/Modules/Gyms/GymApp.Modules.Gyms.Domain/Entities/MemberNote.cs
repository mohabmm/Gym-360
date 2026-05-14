using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class MemberNote : Entity
{
    private MemberNote() : base() { }

    private MemberNote(
        Guid id,
        Guid gymId,
        Guid memberId,
        Guid createdBy,
        string content) : base(id)
    {
        GymId = gymId;
        MemberId = memberId;
        CreatedBy = createdBy;
        Content = content;
    }

    public Guid GymId { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string Content { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public static MemberNote Create(
        Guid gymId,
        Guid memberId,
        Guid createdBy,
        string content)
    {
        var note = new MemberNote(
            Guid.NewGuid(),
            gymId,
            memberId,
            createdBy,
            content);
        note.CreatedAt = DateTime.UtcNow;
        return note;
    }
}
