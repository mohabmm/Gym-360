using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gamification.Application.DTOs;

namespace GymApp.Modules.Gamification.Application.Queries.GetMyLeaderboardPosition;

public sealed record GetMyLeaderboardPositionQuery(
    Guid UserId,
    Guid GymId) : IQuery<MyLeaderboardPositionResponse>;
