namespace GymApp.Modules.Gyms.Domain.ValueObjects;

public sealed record ClassSchedule(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    int DurationMinutes,
    bool IsRecurring);
