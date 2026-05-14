using System.ComponentModel.DataAnnotations;

namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record ClassScheduleDto(
    [property: Required] DayOfWeek DayOfWeek,
    [property: Required] TimeOnly StartTime,
    [property: Required, Range(1, 480)] int DurationMinutes,
    [property: Required] bool IsRecurring);
