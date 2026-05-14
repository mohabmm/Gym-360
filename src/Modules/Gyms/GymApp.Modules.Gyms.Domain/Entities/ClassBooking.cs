using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Enums;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class ClassBooking : Entity
{
    private ClassBooking() : base() { }

    private ClassBooking(
        Guid id,
        Guid gymClassId,
        Guid memberId,
        BookingStatus status) : base(id)
    {
        GymClassId = gymClassId;
        MemberId = memberId;
        Status = status;
        BookedAt = DateTime.UtcNow;
    }

    public Guid GymClassId { get; private set; }
    public Guid MemberId { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTime BookedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public static ClassBooking Create(Guid gymClassId, Guid memberId, BookingStatus status)
    {
        return new ClassBooking(Guid.NewGuid(), gymClassId, memberId, status);
    }

    public Result Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            return Result.Failure(Errors.GymErrors.BookingNotFound);

        Status = BookingStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void MarkAttended()
    {
        Status = BookingStatus.Attended;
    }

    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
    }
}
