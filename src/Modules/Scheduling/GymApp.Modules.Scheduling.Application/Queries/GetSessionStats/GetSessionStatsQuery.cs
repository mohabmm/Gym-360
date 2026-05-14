using GymApp.Common.Application.Messaging;
using GymApp.Modules.Scheduling.Application.DTOs;

namespace GymApp.Modules.Scheduling.Application.Queries.GetSessionStats;

public sealed record GetSessionStatsQuery(
    Guid TrainerId,
    Guid GymId,
    DateOnly? From,
    DateOnly? To) : IQuery<SessionStatsResponse>;
