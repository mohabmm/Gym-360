using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymClassById;

public sealed record GetGymClassByIdQuery(Guid GymId, Guid ClassId) : IQuery<GymClassDto>;
