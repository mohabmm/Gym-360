namespace GymApp.Modules.Gamification.Application.DTOs;

public sealed record LeaderboardResponse(
    string Type,
    string Period,
    IReadOnlyList<LeaderboardEntryDto> Entries,
    LeaderboardEntryDto? MyPosition,
    int TotalParticipants,
    int OptedOutCount);
