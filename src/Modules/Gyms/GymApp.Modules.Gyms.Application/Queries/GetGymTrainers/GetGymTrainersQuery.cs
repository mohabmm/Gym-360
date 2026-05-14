using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymTrainers;

public sealed record GetGymTrainersQuery(Guid GymId, string? Status = null) : IQuery<List<TrainerDto>>;
