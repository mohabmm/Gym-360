using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymStaff;

public sealed record GetGymStaffQuery(Guid GymId) : IQuery<List<StaffDto>>;
