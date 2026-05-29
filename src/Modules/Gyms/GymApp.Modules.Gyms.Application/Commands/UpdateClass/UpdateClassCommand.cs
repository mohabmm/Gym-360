using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateClass;

public sealed record UpdateClassCommand(
    Guid GymId,
    Guid ClassId,
    string Name,
    string? Description,
    ClassScheduleDto Schedule,
    int Capacity,
    bool WaitlistEnabled) : ICommand;
