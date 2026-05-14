namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record GymSeasonalHoursDto(
    Guid Id,
    string Name,
    string StartDate,
    string EndDate,
    bool IsActive,
    List<DayScheduleDto> DaySchedules);
