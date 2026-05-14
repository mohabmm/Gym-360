namespace GymApp.Modules.Gamification.Application.DTOs;

public sealed record LeaderboardEntryDto(
    int Position,
    Guid MemberId,
    string MemberName,
    string? MemberPhotoUrl,
    decimal Score,
    string? ScoreLabel);
