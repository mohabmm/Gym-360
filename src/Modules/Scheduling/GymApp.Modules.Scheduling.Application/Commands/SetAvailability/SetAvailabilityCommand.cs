using GymApp.Common.Application.Messaging;
using GymApp.Modules.Scheduling.Application.DTOs;

namespace GymApp.Modules.Scheduling.Application.Commands.SetAvailability;

public sealed record SetAvailabilityCommand(
    Guid TrainerId,
    Guid GymId,
    int SessionDurationMinutes,
    int BufferMinutes,
    List<AvailabilitySlotDto> WeeklySlots) : ICommand;
