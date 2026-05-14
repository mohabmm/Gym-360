using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetClassWaitlist;

public sealed record GetClassWaitlistQuery(Guid GymId, Guid ClassId) : IQuery<List<MemberDto>>;
