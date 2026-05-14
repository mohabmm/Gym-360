using GymApp.Common.Application.Messaging;
using GymApp.Modules.Scheduling.Application.DTOs;

namespace GymApp.Modules.Scheduling.Application.Queries.GetSessionDetail;

public sealed record GetSessionDetailQuery(Guid SessionId) : IQuery<SessionDetailDto>;
