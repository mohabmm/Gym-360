using GymApp.Common.Application.Messaging;
using GymApp.Modules.Scheduling.Application.DTOs;

namespace GymApp.Modules.Scheduling.Application.Queries.GetWeeklyAvailability;

public sealed record GetWeeklyAvailabilityQuery(
    Guid TrainerId,
    Guid GymId,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? RequestingMemberId) : IQuery<WeeklyAvailabilityDto>;
