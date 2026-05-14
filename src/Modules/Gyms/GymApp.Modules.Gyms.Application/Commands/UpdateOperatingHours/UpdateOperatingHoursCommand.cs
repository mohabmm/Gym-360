using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateOperatingHours;

public sealed record UpdateOperatingHoursCommand(
    Guid GymId,
    List<DayScheduleDto> Hours) : ICommand;
