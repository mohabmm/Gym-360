using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetGym;

public sealed record GetGymQuery(Guid GymId) : IQuery<GymResponse>;
