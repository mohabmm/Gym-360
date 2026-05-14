using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetOperatingHours;

public sealed record GetOperatingHoursQuery(Guid GymId) : IQuery<List<DayScheduleDto>>;
