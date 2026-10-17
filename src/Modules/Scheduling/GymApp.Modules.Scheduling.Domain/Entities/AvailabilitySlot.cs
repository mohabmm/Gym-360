using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.Domain.Entities;

public sealed class AvailabilitySlot : Entity
{
    private AvailabilitySlot() : base() { }

    private AvailabilitySlot(Guid id) : base(id) { }

    public Guid TrainerAvailabilityId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsActive { get; private set; }

    public static AvailabilitySlot Create(
        Guid trainerAvailabilityId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        return new AvailabilitySlot(Guid.NewGuid())
        {
            TrainerAvailabilityId = trainerAvailabilityId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            IsActive = true
        };
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void UpdateTimes(TimeOnly startTime, TimeOnly endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }

    public bool OverlapsWith(AvailabilitySlot other)
    {
        if (DayOfWeek != other.DayOfWeek)
            return false;

        return StartTime < other.EndTime && other.StartTime < EndTime;
    }
}
