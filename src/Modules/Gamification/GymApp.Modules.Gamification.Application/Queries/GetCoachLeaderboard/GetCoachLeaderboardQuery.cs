using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gamification.Application.DTOs;

namespace GymApp.Modules.Gamification.Application.Queries.GetCoachLeaderboard;

public sealed record GetCoachLeaderboardQuery(Guid GymId) : IQuery<List<CoachLeaderboardEntryDto>>;
