namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record DayScheduleDto(
    string DayOfWeek,
    string OpenTime,
    string CloseTime,
    bool IsClosed);
