using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymMemberById;

public sealed record GetGymMemberByIdQuery(Guid GymId, Guid MemberId) : IQuery<MemberDto>;
