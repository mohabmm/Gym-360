using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetSessionPolicy;

public sealed record GetSessionPolicyQuery(Guid GymId) : IQuery<SessionPolicyDto>;
