using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.Errors;

namespace GymApp.Modules.Scheduling.Domain.Entities;

public sealed class TrainerAvailability : Entity
{
    private readonly List<AvailabilitySlot> _weeklySlots = new();
    private readonly List<BlockedDate> _blockedDates = new();

    private TrainerAvailability() : base() { }

    private TrainerAvailability(
        Guid id,
        Guid trainerId,
        Guid gymId,
        int sessionDurationMinutes,
        int bufferMinutes) : base(id)
    {
        TrainerId = trainerId;
        GymId = gymId;
        SessionDurationMinutes = sessionDurationMinutes;
        BufferMinutes = bufferMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid TrainerId { get; private set; }
    public Guid GymId { get; private set; }
    public int SessionDurationMinutes { get; private set; }
    public int BufferMinutes { get; private set; }
    public IReadOnlyList<AvailabilitySlot> WeeklySlots => _weeklySlots.AsReadOnly();
    public IReadOnlyList<BlockedDate> BlockedDates => _blockedDates.AsReadOnly();
    public DateTime UpdatedAt { get; private set; }

    public static TrainerAvailability Create(
        Guid trainerId,
        Guid gymId,
        int sessionDurationMinutes = 60,
        int bufferMinutes = 15)
    {
        return new TrainerAvailability(
            Guid.NewGuid(),
            trainerId,
            gymId,
            sessionDurationMinutes,
            bufferMinutes);
    }

    public Result AddSlot(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime)
    {
        var newSlot = AvailabilitySlot.Create(Id, dayOfWeek, startTime, endTime);

        foreach (var existing in _weeklySlots.Where(s => s.IsActive))
        {
            if (existing.OverlapsWith(newSlot))
                return Result.Failure(SchedulingErrors.SlotOverlap);
        }

        _weeklySlots.Add(newSlot);
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RemoveSlot(Guid slotId)
    {
        var slot = _weeklySlots.FirstOrDefault(s => s.Id == slotId);
        if (slot is null)
            return Result.Failure(SchedulingErrors.SlotNotAvailable);

        _weeklySlots.Remove(slot);
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result BlockDate(DateOnly date, string? reason, bool autoNotifyClients = false)
    {
        if (_blockedDates.Any(b => b.Date == date))
            return Result.Failure(SchedulingErrors.DateAlreadyBlocked);

        var blockedDate = BlockedDate.Create(Id, date, reason, autoNotifyClients);
        _blockedDates.Add(blockedDate);
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result UnblockDate(DateOnly date)
    {
        var blocked = _blockedDates.FirstOrDefault(b => b.Date == date);
        if (blocked is null)
            return Result.Failure(SchedulingErrors.AvailabilityNotFound);

        _blockedDates.Remove(blocked);
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void UpdateSessionDuration(int durationMinutes, int bufferMinutes)
    {
        SessionDurationMinutes = durationMinutes;
        BufferMinutes = bufferMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsDateBlocked(DateOnly date)
    {
        return _blockedDates.Any(b => b.Date == date);
    }

    public List<AvailabilitySlot> ActiveSlotsOn(DayOfWeek dayOfWeek) =>
        _weeklySlots.Where(s => s.IsActive && s.DayOfWeek == dayOfWeek).ToList();

    public bool HasAvailableSlot(DayOfWeek dayOfWeek, TimeOnly time)
    {
        return _weeklySlots.Any(s =>
            s.IsActive &&
            s.DayOfWeek == dayOfWeek &&
            s.StartTime <= time &&
            s.EndTime >= time.AddMinutes(SessionDurationMinutes));
    }
}
