using GymApp.Common.Domain;

namespace GymApp.Modules.Gamification.Domain.Entities;

public sealed class LeaderboardOptOut : Entity
{
    private LeaderboardOptOut() { }

    private LeaderboardOptOut(
        Guid id,
        Guid userId,
        Guid gymId,
        bool showOnLeaderboard)
        : base(id)
    {
        UserId = userId;
        GymId = gymId;
        ShowOnLeaderboard = showOnLeaderboard;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public Guid GymId { get; private set; }
    public bool ShowOnLeaderboard { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Result<LeaderboardOptOut> Create(
        Guid userId,
        Guid gymId,
        bool showOnLeaderboard = true)
    {
        var optOut = new LeaderboardOptOut(
            Guid.NewGuid(),
            userId,
            gymId,
            showOnLeaderboard);

        return Result.Success(optOut);
    }

    public void Toggle()
    {
        ShowOnLeaderboard = !ShowOnLeaderboard;
        UpdatedAt = DateTime.UtcNow;
    }
}
