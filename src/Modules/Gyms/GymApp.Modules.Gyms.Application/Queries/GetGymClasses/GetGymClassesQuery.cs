using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymClasses;

public sealed record GetGymClassesQuery(Guid GymId) : IQuery<List<GymClassDto>>;
