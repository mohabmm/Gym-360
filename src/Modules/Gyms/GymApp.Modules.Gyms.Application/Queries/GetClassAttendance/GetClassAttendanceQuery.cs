using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetClassAttendance;

public sealed record GetClassAttendanceQuery(Guid GymId, Guid ClassId) : IQuery<List<MemberDto>>;
