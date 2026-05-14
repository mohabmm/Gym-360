using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Events;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class CheckIn : Entity
{
    private CheckIn() : base() { }

    private CheckIn(
        Guid id,
        Guid gymId,
        Guid memberId,
        CheckInMethod method,
        Guid? staffId) : base(id)
    {
        GymId = gymId;
        MemberId = memberId;
        Method = method;
        StaffId = staffId;
        CheckedInAt = DateTime.UtcNow;
    }

    public Guid GymId { get; private set; }
    public Guid MemberId { get; private set; }
    public CheckInMethod Method { get; private set; }
    public DateTime CheckedInAt { get; private set; }
    public Guid? StaffId { get; private set; }

    public static CheckIn Create(Guid gymId, Guid memberId, CheckInMethod method, Guid? staffId = null)
    {
        var checkIn = new CheckIn(Guid.NewGuid(), gymId, memberId, method, staffId);
        checkIn.RaiseDomainEvent(new CheckInRecordedDomainEvent(gymId, memberId, method.ToString()));
        return checkIn;
    }
}
