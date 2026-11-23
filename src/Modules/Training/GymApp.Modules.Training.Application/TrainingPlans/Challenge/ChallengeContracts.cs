using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Training.Application.TrainingPlans.Challenge;

/// <summary>The trainee's own first-month challenge: progress, the reward, and whether it was reached.</summary>
public sealed record GetMyChallengeQuery(PlanActor Actor) : IQuery<ChallengeStatusDto>;

/// <summary>Desk list: members who earned the reward; <paramref name="Delivered"/> null = all.</summary>
public sealed record ListChallengeAwardsQuery(PlanActor Actor, bool? Delivered) : IQuery<IReadOnlyList<ChallengeAwardDto>>;

/// <summary>The desk ticks (or unticks) "reward delivered".</summary>
public sealed record SetChallengeAwardDeliveredCommand(PlanActor Actor, Guid AwardId, bool Delivered) : ICommand<ChallengeAwardDto>;

/// <summary>Owner dashboard (pt.owner_dashboard): plans, workouts and the challenge at a glance.</summary>
public sealed record GetPlanSummaryQuery(PlanActor Actor) : IQuery<PlanSummaryDto>;

public static class ChallengeStates
{
    /// <summary>Inside the window, target not reached yet.</summary>
    public const string Active = "active";
    public const string Reached = "reached";
    /// <summary>The window is over without reaching the target.</summary>
    public const string Ended = "ended";
    /// <summary>No join date and no plan yet, so the window has not started.</summary>
    public const string NotStarted = "not_started";
}

public sealed record ChallengeStatusDto
{
    public required string Status { get; init; }
    public required int Count { get; init; }
    public required int Target { get; init; }
    public DateOnly? WindowStart { get; init; }
    public DateOnly? WindowEnd { get; init; }
    public int DaysLeft { get; init; }
    public required string RewardTextAr { get; init; }
    public required string RewardTextEn { get; init; }
    public DateTime? ReachedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
}

public sealed record ChallengeAwardDto
{
    public required Guid AwardId { get; init; }
    public required Guid TraineeId { get; init; }
    public required string TraineeName { get; init; }
    public Guid? TrainerId { get; init; }
    public string? TrainerName { get; init; }
    public required int Count { get; init; }
    public required int Target { get; init; }
    public required DateOnly WindowStart { get; init; }
    public required DateOnly WindowEnd { get; init; }
    public required DateTime ReachedAt { get; init; }
    public required string RewardTextAr { get; init; }
    public required string RewardTextEn { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public string? DeliveredByName { get; init; }
}

public sealed record PlanSummaryDto
{
    /// <summary>Members with an active plan right now.</summary>
    public required int ActivePlans { get; init; }
    /// <summary>Plan workouts finished this gym-local month (reversed sessions excluded).</summary>
    public required int WorkoutsThisMonth { get; init; }
    /// <summary>Members who finished at least one plan workout this month.</summary>
    public required int MembersTrainingThisMonth { get; init; }
    /// <summary>False when mem.first_month_challenge is off; the challenge numbers are 0 then.</summary>
    public required bool ChallengeEnabled { get; init; }
    public int ChallengeReached { get; init; }
    public int ChallengeAwaitingDelivery { get; init; }
}
