namespace GymApp.Modules.Gamification.Application.DTOs;

public sealed record CoachOfTheMonthResponse(
    Guid? TrainerId,
    int Month,
    int Year,
    decimal? NormalizedScore,
    int? TotalRatings,
    int? UniqueClients,
    int? SessionCount,
    bool HasCoach);
