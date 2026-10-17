namespace GymApp.Modules.Gamification.Domain.ValueObjects;

/// <summary>One member's standing on a leaderboard: rank, score, movement and visibility.</summary>
public sealed record LeaderboardEntryDetails
{
    public required Guid MemberId { get; init; }
    public required int Rank { get; init; }
    public required decimal Score { get; init; }
    public decimal? Delta { get; init; }
    public required bool IsOptedIn { get; init; }
}
