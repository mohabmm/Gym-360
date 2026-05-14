using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetTrainerRate;

public sealed record GetTrainerRateQuery(
    Guid GymId,
    Guid TrainerId) : IQuery<TrainerRateDto>;
