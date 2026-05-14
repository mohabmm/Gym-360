using GymApp.Common.Application.Messaging;
using GymApp.Modules.Scheduling.Application.DTOs;

namespace GymApp.Modules.Scheduling.Application.Queries.GetAvailability;

public sealed record GetAvailabilityQuery(
    Guid TrainerId,
    Guid GymId) : IQuery<AvailabilityResponse>;
