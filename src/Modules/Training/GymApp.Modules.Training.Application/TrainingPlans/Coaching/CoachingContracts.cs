using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Application.TrainingPlans.Coaching;

/// <summary>The gym's plan settings (defaults when never changed).</summary>
public sealed record GetPlanSettingsQuery(PlanActor Actor) : IQuery<PlanSettingsDto>;

/// <summary>
/// Owner or manager only. A null part is left as it is; <paramref name="Challenge"/> needs
/// mem.first_month_challenge to be on.
/// </summary>
public sealed record UpdatePlanSettingsCommand(PlanActor Actor, int? InactiveDays, ChallengeSettings? Challenge = null) : ICommand<PlanSettingsDto>;

/// <summary>Every client of the coach with their plan, this week, last workout, and who needs a nudge.</summary>
public sealed record GetCoachPlanOverviewQuery(PlanActor Actor) : IQuery<CoachPlanOverviewDto>;

/// <summary>Push reminder to a client (once a day per client). <paramref name="Message"/> null = the standard text.</summary>
public sealed record NudgeTraineeCommand(PlanActor Actor, Guid TraineeId, string? Message) : ICommand<NudgeResultDto>;

public sealed record PlanSettingsDto(int InactiveDays, int MinInactiveDays, int MaxInactiveDays)
{
    /// <summary>Null when mem.first_month_challenge is off for the gym.</summary>
    public ChallengeSettingsDto? Challenge { get; init; }
}

/// <summary>The first-month challenge settings, with the allowed ranges and the default reward wording.</summary>
public sealed record ChallengeSettingsDto
{
    public required int Target { get; init; }
    public required int WindowDays { get; init; }
    public required string RewardTextAr { get; init; }
    public required string RewardTextEn { get; init; }
    public bool IsDefaultReward { get; init; }
    public int MinTarget { get; init; } = TrainingPlanSettings.MinChallengeTarget;
    public int MaxTarget { get; init; } = TrainingPlanSettings.MaxChallengeTarget;
    public int MinWindowDays { get; init; } = TrainingPlanSettings.MinChallengeWindowDays;
    public int MaxWindowDays { get; init; } = TrainingPlanSettings.MaxChallengeWindowDays;
    public int MaxRewardLength { get; init; } = ChallengeAward.MaxRewardTextLength;
    public string DefaultRewardTextAr { get; init; } = TrainingPlanSettings.DefaultRewardTextAr;
    public string DefaultRewardTextEn { get; init; } = TrainingPlanSettings.DefaultRewardTextEn;
}

public sealed record NudgeResultDto(DateTime SentAt);

public sealed record CoachClientPlanDto
{
    public required Guid TraineeId { get; init; }
    public Guid? PlanId { get; init; }
    public string? PlanName { get; init; }
    public string? PlanNameAr { get; init; }
    public int ThisWeek { get; init; }
    public int WeekTarget { get; init; }
    public int DoneCount { get; init; }
    public int Total { get; init; }
    public DateOnly? LastWorkoutOn { get; init; }
    /// <summary>Days since the last workout (or since the plan started); null before it starts.</summary>
    public int? DaysSince { get; init; }
    public bool IsInactive { get; init; }
    public string? NextDayTitle { get; init; }
    public string? NextDayTitleAr { get; init; }
}

/// <summary>Clients needing a nudge first (longest first), then the rest with a plan, then those without one.</summary>
public sealed record CoachPlanOverviewDto(int InactiveDays, IReadOnlyList<CoachClientPlanDto> Clients);
