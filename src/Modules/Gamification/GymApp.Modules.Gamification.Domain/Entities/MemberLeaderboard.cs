using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Domain.DomainEvents;
using GymApp.Modules.Gamification.Domain.Enums;
using GymApp.Modules.Gamification.Domain.ValueObjects;

namespace GymApp.Modules.Gamification.Domain.Entities;

public sealed class MemberLeaderboard : Entity
{
    private readonly List<LeaderboardEntry> _entries = new();

    private MemberLeaderboard() { }

    private MemberLeaderboard(
        Guid id,
        Guid gymId,
        LeaderboardPeriod period,
        LeaderboardType leaderboardType)
        : base(id)
    {
        GymId = gymId;
        Period = period;
        LeaderboardType = leaderboardType;
        RecalculatedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid GymId { get; private set; }
    public LeaderboardPeriod Period { get; private set; } = null!;
    public LeaderboardType LeaderboardType { get; private set; }
    public IReadOnlyList<LeaderboardEntry> Entries => _entries.AsReadOnly();
    public DateTime RecalculatedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Result<MemberLeaderboard> Create(
        Guid gymId,
        LeaderboardPeriod period,
        LeaderboardType leaderboardType)
    {
        var leaderboard = new MemberLeaderboard(
            Guid.NewGuid(),
            gymId,
            period,
            leaderboardType);

        return Result.Success(leaderboard);
    }

    public LeaderboardEntry AddEntry(
        Guid memberId,
        int rank,
        decimal score,
        decimal? delta,
        bool isOptedIn)
    {
        var entry = LeaderboardEntry.Create(
            Id,
            memberId,
            rank,
            score,
            delta,
            isOptedIn);

        _entries.Add(entry);
        return entry;
    }

    public void ClearEntries()
    {
        _entries.Clear();
    }

    public void MarkRecalculated()
    {
        RecalculatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new LeaderboardRecalculatedDomainEvent(
            Id,
            GymId,
            LeaderboardType.ToString()));
    }
}
