namespace GymApp.Modules.Gamification.Application.DTOs;

public sealed record CoachLeaderboardEntryDto(
    Guid TrainerId,
    decimal NormalizedScore,
    int TotalRatings,
    int UniqueClients,
    int SessionCount,
    bool IsCoachOfTheMonth);
