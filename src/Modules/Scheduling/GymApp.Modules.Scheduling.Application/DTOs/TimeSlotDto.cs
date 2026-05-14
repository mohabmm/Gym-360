namespace GymApp.Modules.Scheduling.Application.DTOs;

public sealed record TimeSlotDto(
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsAvailable);
