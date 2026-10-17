using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Domain.ValueObjects;

namespace GymApp.Modules.Gamification.Domain.Entities;

public sealed class LeaderboardEntry : Entity
{
    private LeaderboardEntry() { }

    private LeaderboardEntry(Guid id) : base(id) { }

    public Guid LeaderboardId { get; private set; }
    public Guid MemberId { get; private set; }
    public int Rank { get; private set; }
    public decimal Score { get; private set; }
    public decimal? Delta { get; private set; }
    public bool IsOptedIn { get; private set; }

    public static LeaderboardEntry Create(Guid leaderboardId, LeaderboardEntryDetails details)
    {
        return new LeaderboardEntry(Guid.NewGuid())
        {
            LeaderboardId = leaderboardId,
            MemberId = details.MemberId,
            Rank = details.Rank,
            Score = details.Score,
            Delta = details.Delta,
            IsOptedIn = details.IsOptedIn
        };
    }

    public void UpdateRank(int newRank, decimal newScore, decimal? newDelta)
    {
        Rank = newRank;
        Score = newScore;
        Delta = newDelta;
    }

    public void OptOut()
    {
        IsOptedIn = false;
    }

    public void OptIn()
    {
        IsOptedIn = true;
    }
}
