using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateSeasonalHours;

public sealed record SeasonalOverrideInput(
    string Name,
    string StartDate,
    string EndDate,
    bool IsActive,
    List<DayScheduleDto>? DaySchedules);

public sealed record UpdateSeasonalHoursCommand(
    Guid GymId,
    List<SeasonalOverrideInput> Overrides) : ICommand;
