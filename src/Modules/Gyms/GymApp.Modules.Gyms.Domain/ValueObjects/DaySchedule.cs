namespace GymApp.Modules.Gyms.Domain.ValueObjects;

public sealed record DaySchedule(
    DayOfWeek DayOfWeek,
    TimeOnly OpenTime,
    TimeOnly CloseTime,
    bool IsClosed);
