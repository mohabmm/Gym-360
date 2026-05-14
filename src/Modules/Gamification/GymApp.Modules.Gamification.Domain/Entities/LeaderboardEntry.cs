using GymApp.Common.Domain;

namespace GymApp.Modules.Gamification.Domain.Entities;

public sealed class LeaderboardEntry : Entity
{
    private LeaderboardEntry() { }

    private LeaderboardEntry(
        Guid id,
        Guid leaderboardId,
        Guid memberId,
        int rank,
        decimal score,
        decimal? delta,
        bool isOptedIn)
        : base(id)
    {
        LeaderboardId = leaderboardId;
        MemberId = memberId;
        Rank = rank;
        Score = score;
        Delta = delta;
        IsOptedIn = isOptedIn;
    }

    public Guid LeaderboardId { get; private set; }
    public Guid MemberId { get; private set; }
    public int Rank { get; private set; }
    public decimal Score { get; private set; }
    public decimal? Delta { get; private set; }
    public bool IsOptedIn { get; private set; }

    public static LeaderboardEntry Create(
        Guid leaderboardId,
        Guid memberId,
        int rank,
        decimal score,
        decimal? delta,
        bool isOptedIn)
    {
        return new LeaderboardEntry(
            Guid.NewGuid(),
            leaderboardId,
            memberId,
            rank,
            score,
            delta,
            isOptedIn);
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
