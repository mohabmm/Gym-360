using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymTrainerById;

public sealed record GetGymTrainerByIdQuery(Guid GymId, Guid TrainerId) : IQuery<TrainerDto>;
