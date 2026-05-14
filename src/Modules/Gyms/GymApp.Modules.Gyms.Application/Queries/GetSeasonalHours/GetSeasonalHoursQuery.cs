using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetSeasonalHours;

public sealed record GetSeasonalHoursQuery(Guid GymId) : IQuery<List<GymSeasonalHoursDto>>;
