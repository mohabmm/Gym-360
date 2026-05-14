using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gamification.Application.DTOs;

namespace GymApp.Modules.Gamification.Application.Queries.GetCoachOfTheMonth;

public sealed record GetCoachOfTheMonthQuery(
    Guid GymId,
    int Month,
    int Year) : IQuery<CoachOfTheMonthResponse>;
