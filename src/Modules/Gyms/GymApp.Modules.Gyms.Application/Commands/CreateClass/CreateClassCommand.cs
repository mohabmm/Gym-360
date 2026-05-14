using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Commands.CreateClass;

public sealed record CreateClassCommand(
    Guid GymId,
    string Name,
    string? Description,
    Guid TrainerId,
    ClassScheduleDto Schedule,
    int Capacity,
    bool WaitlistEnabled) : ICommand<Guid>;
