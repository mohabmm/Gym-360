using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gamification.Application.DTOs;

namespace GymApp.Modules.Gamification.Application.Queries.GetTrainerMonthlyScores;

public sealed record GetTrainerMonthlyScoresQuery(
    Guid GymId,
    int Month,
    int Year) : IQuery<List<TrainerMonthlyScoreDto>>;
