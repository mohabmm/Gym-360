using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Events;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class GymClass : Entity
{
    private readonly List<ClassBooking> _bookings = new();

    private GymClass() : base() { }

    private GymClass(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid TrainerId { get; private set; }
    public ClassSchedule Schedule { get; private set; } = default!;
    public int Capacity { get; private set; }
    public bool WaitlistEnabled { get; private set; }
    public GymClassStatus Status { get; private set; }
    public IReadOnlyList<ClassBooking> Bookings => _bookings.AsReadOnly();
    public DateTime CreatedAt { get; private set; }

    public static GymClass Create(Guid gymId, Guid trainerId, GymClassDetails details)
    {
        var gymClass = new GymClass(Guid.NewGuid())
        {
            GymId = gymId,
            Name = details.Name,
            Description = details.Description,
            TrainerId = trainerId,
            Schedule = details.Schedule,
            Capacity = details.Capacity,
            WaitlistEnabled = details.WaitlistEnabled,
            Status = GymClassStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        gymClass.RaiseDomainEvent(new ClassCreatedDomainEvent(gymId, gymClass.Id));
        return gymClass;
    }

    public Result<ClassBooking> Book(Guid memberId)
    {
        if (Status != GymClassStatus.Active)
            return Result.Failure<ClassBooking>(Errors.GymErrors.ClassNotFound);

        var existingBooking = _bookings.FirstOrDefault(b =>
            b.MemberId == memberId &&
            b.Status != BookingStatus.Cancelled);

        if (existingBooking is not null)
            return Result.Failure<ClassBooking>(Errors.GymErrors.ClassAlreadyBooked);

        var confirmedCount = _bookings.Count(b => b.Status == BookingStatus.Confirmed);

        BookingStatus status;
        if (confirmedCount >= Capacity)
        {
            if (!WaitlistEnabled)
                return Result.Failure<ClassBooking>(Errors.GymErrors.ClassFull);

            status = BookingStatus.Waitlisted;
        }
        else
        {
            status = BookingStatus.Confirmed;
        }

        var booking = ClassBooking.Create(Id, memberId, status);
        _bookings.Add(booking);

        RaiseDomainEvent(new ClassBookedDomainEvent(GymId, Id, memberId));
        return Result.Success(booking);
    }

    public Result Cancel()
    {
        if (Status == GymClassStatus.Cancelled)
            return Result.Failure(Errors.GymErrors.ClassNotFound);

        Status = GymClassStatus.Cancelled;
        RaiseDomainEvent(new ClassCancelledDomainEvent(GymId, Id));
        return Result.Success();
    }

    public void Complete()
    {
        Status = GymClassStatus.Completed;
    }

    public void Update(GymClassDetails details)
    {
        Name = details.Name;
        Description = details.Description;
        Schedule = details.Schedule;
        Capacity = details.Capacity;
        WaitlistEnabled = details.WaitlistEnabled;
    }
}
