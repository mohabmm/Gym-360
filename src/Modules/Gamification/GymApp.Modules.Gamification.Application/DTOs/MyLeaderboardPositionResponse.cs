namespace GymApp.Modules.Gamification.Application.DTOs;

public sealed record MyLeaderboardPositionResponse(
    List<LeaderboardPositionDto> Positions);

public sealed record LeaderboardPositionDto(
    string LeaderboardType,
    int? Rank,
    decimal? Score,
    decimal? Delta,
    bool IsOptedIn);
