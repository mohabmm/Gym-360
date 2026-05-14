namespace GymApp.Modules.Gamification.Application.DTOs;

public sealed record TrainerMonthlyScoreDto(
    Guid TrainerId,
    int Month,
    int Year,
    decimal AggregatedScore,
    int TotalRatings,
    int UniqueClients,
    int SessionCount,
    decimal NormalizedScore,
    bool IsCoachOfTheMonth,
    DateTime CalculatedAt);
