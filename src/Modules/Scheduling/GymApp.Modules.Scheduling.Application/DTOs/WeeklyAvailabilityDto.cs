namespace GymApp.Modules.Scheduling.Application.DTOs;

public sealed record WeeklyAvailabilityDto(
    Guid TrainerId,
    Guid GymId,
    DateOnly StartDate,
    DateOnly EndDate,
    int SessionDurationMinutes,
    int BufferMinutes,
    List<DayAvailabilityDto> Days);

public sealed record DayAvailabilityDto(
    DateOnly Date,
    DayOfWeek DayOfWeek,
    bool IsOffDay,
    bool IsBlocked,
    string? BlockedReason,
    List<WeekSlotDto> Slots);

public sealed record WeekSlotDto(
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status,
    Guid? SessionId);
