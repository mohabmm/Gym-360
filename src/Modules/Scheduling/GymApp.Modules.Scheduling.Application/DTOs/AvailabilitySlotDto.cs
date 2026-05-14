namespace GymApp.Modules.Scheduling.Application.DTOs;

public sealed record AvailabilitySlotDto(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);
