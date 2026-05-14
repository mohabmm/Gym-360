using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Events;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class GymMember : Entity
{
    private GymMember() : base() { }

    private GymMember(Guid id, Guid gymId, Guid memberId, string memberNumber, string? section)
        : base(id)
    {
        GymId = gymId;
        MemberId = memberId;
        MemberNumber = memberNumber;
        Section = section;
        JoinedAt = DateTime.UtcNow;
    }

    public Guid GymId { get; private set; }
    public Guid MemberId { get; private set; }
    public string MemberNumber { get; private set; } = default!;
    public string? Section { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public bool IsFrozen { get; private set; }
    public DateTime? FrozenAt { get; private set; }

    public static GymMember Create(Guid gymId, Guid memberId, string memberNumber, string? section)
        => new(Guid.NewGuid(), gymId, memberId, memberNumber, section);

    public void SetSection(string? section) => Section = section;

    public Result Freeze()
    {
        if (IsFrozen)
            return Result.Failure(GymErrors.MemberAlreadyFrozen);

        IsFrozen = true;
        FrozenAt = DateTime.UtcNow;
        RaiseDomainEvent(new MemberFrozenDomainEvent(GymId, MemberId));
        return Result.Success();
    }

    public Result Unfreeze()
    {
        if (!IsFrozen)
            return Result.Failure(GymErrors.MemberNotFrozen);

        IsFrozen = false;
        FrozenAt = null;
        RaiseDomainEvent(new MemberUnfrozenDomainEvent(GymId, MemberId));
        return Result.Success();
    }
}
