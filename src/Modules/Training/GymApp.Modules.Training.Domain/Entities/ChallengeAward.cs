using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Entities;

/// <summary>
/// mem.first_month_challenge: a member reached the target in their window. One per member per
/// gym (unique index), which is what keeps the "you did it" notifications to a single send.
/// The award stays even if a counted session is later reversed. The reward itself is handed
/// over in person; the desk ticks it off here.
/// </summary>
public sealed class ChallengeAward : Entity
{
    public const int MaxRewardTextLength = 300;

    private ChallengeAward() { }

    private ChallengeAward(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    public Guid TraineeId { get; private set; }
    /// <summary>The coach of the member's plan when the target was reached.</summary>
    public Guid? TrainerId { get; private set; }
    public Guid? PlanId { get; private set; }
    public DateOnly WindowStart { get; private set; }
    public DateOnly WindowEnd { get; private set; }
    public int Target { get; private set; }
    public int CountAtReach { get; private set; }
    public DateTime ReachedAt { get; private set; }
    /// <summary>The reward as the gym worded it at the time, so a later settings change cannot rewrite it.</summary>
    public string RewardTextAr { get; private set; } = default!;
    public string RewardTextEn { get; private set; } = default!;
    public DateTime? DeliveredAt { get; private set; }
    public Guid? DeliveredBy { get; private set; }

    public bool IsDelivered => DeliveredAt is not null;

    public static ChallengeAward Reach(ChallengeReach reach) => new(Guid.NewGuid())
    {
        GymId = reach.GymId,
        TraineeId = reach.TraineeId,
        TrainerId = reach.TrainerId,
        PlanId = reach.PlanId,
        WindowStart = reach.Window.Start,
        WindowEnd = reach.Window.End,
        Target = reach.Target,
        CountAtReach = reach.Count,
        ReachedAt = DateTime.UtcNow,
        RewardTextAr = reach.RewardTextAr,
        RewardTextEn = reach.RewardTextEn
    };

    /// <summary>The desk ticks "reward delivered" (or unticks a mistake).</summary>
    public void SetDelivered(bool delivered, Guid by)
    {
        if (delivered == IsDelivered)
            return;
        DeliveredAt = delivered ? DateTime.UtcNow : null;
        DeliveredBy = delivered ? by : null;
    }
}

/// <summary>Everything known at the moment the member reached the target.</summary>
public sealed record ChallengeReach
{
    public required Guid GymId { get; init; }
    public required Guid TraineeId { get; init; }
    public Guid? TrainerId { get; init; }
    public Guid? PlanId { get; init; }
    public required ValueObjects.ChallengeWindow Window { get; init; }
    public required int Target { get; init; }
    public required int Count { get; init; }
    public required string RewardTextAr { get; init; }
    public required string RewardTextEn { get; init; }
}
