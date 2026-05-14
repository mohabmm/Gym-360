using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gamification.Application.DTOs;

namespace GymApp.Modules.Gamification.Application.Queries.GetMemberLeaderboard;

public sealed record GetMemberLeaderboardQuery(
    Guid GymId,
    string Type,
    int? Month,
    int? Year,
    Guid? RequestingUserId) : IQuery<LeaderboardResponse>;
