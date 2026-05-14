using GymApp.Common.Application.Messaging;
using GymApp.Modules.Scheduling.Application.DTOs;

namespace GymApp.Modules.Scheduling.Application.Queries.GetAvailableSlots;

public sealed record GetAvailableSlotsQuery(
    Guid TrainerId,
    Guid GymId,
    DateOnly Date) : IQuery<List<TimeSlotDto>>;
