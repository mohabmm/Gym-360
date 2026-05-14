using GymApp.Common.Application.Messaging;
using GymApp.Modules.Scheduling.Application.DTOs;

namespace GymApp.Modules.Scheduling.Application.Queries.GetUpcomingSessions;

public sealed record GetUpcomingSessionsQuery(
    Guid UserId,
    Guid GymId,
    string Role) : IQuery<List<SessionDto>>;
