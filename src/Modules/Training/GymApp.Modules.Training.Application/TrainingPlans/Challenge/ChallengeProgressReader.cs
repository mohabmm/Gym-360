using GymApp.Modules.Gyms.Shared;
using GymApp.Modules.Training.Application.TrainingPlans.Workouts;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.TrainingPlans.Challenge;

/// <summary>Where a member stands in the first-month challenge, from the gym's current settings.</summary>
internal sealed record ChallengeProgress
{
    public required TrainingPlanSettings Settings { get; init; }
    /// <summary>Null when there is neither a join date nor a plan yet.</summary>
    public ChallengeWindow? Window { get; init; }
    public int Count { get; init; }
    /// <summary>The active plan (or the latest one): its coach is the member's coach.</summary>
    public TrainingPlan? CoachPlan { get; init; }

    public bool IsReached => Window is not null && Count >= Settings.ChallengeTarget;

    public string StatusOn(DateOnly today)
    {
        if (Window is null)
            return ChallengeStates.NotStarted;
        if (IsReached)
            return ChallengeStates.Reached;
        return Window.HasEnded(today) ? ChallengeStates.Ended : ChallengeStates.Active;
    }

    public ChallengeReach ToReach(Guid gymId, Guid traineeId) => new()
    {
        GymId = gymId,
        TraineeId = traineeId,
        TrainerId = CoachPlan?.TrainerId,
        PlanId = CoachPlan?.Id,
        Window = Window!,
        Target = Settings.ChallengeTarget,
        Count = Count,
        RewardTextAr = Settings.EffectiveRewardTextAr,
        RewardTextEn = Settings.EffectiveRewardTextEn
    };
}

internal sealed class ChallengeProgressReader
{
    private readonly IGymMemberJoinDateLookup _joinDates;
    private readonly ITrainingPlanSettingsRepository _settings;
    private readonly ITrainingPlanRepository _plans;
    private readonly IPlanCompletionRepository _completions;

    public ChallengeProgressReader(
        IGymMemberJoinDateLookup joinDates,
        ITrainingPlanSettingsRepository settings,
        ITrainingPlanRepository plans,
        IPlanCompletionRepository completions)
    {
        _joinDates = joinDates;
        _settings = settings;
        _plans = plans;
        _completions = completions;
    }

    public async Task<ChallengeProgress> ReadAsync(Guid gymId, Guid traineeId, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(gymId, cancellationToken) ?? TrainingPlanSettings.ForGym(gymId);
        var plans = await _plans.ListForTraineeAsync(gymId, traineeId, cancellationToken);
        var joinedAt = await _joinDates.GetJoinedAtAsync(gymId, traineeId, cancellationToken);
        var joinedOn = joinedAt is { } utc ? TrainingCalendar.LocalDate(utc) : (DateOnly?)null;
        var firstStart = plans.Count == 0 ? (DateOnly?)null : plans.Min(p => p.StartDate);
        var window = ChallengeWindow.For(joinedOn, firstStart, settings.ChallengeWindowDays);
        var count = window is null ? 0 : await CountAsync(gymId, traineeId, window, cancellationToken);
        // ListForTraineeAsync is newest first, so without an active plan this is the latest one.
        var coachPlan = plans.FirstOrDefault(p => p.IsActive) ?? plans.FirstOrDefault();
        return new ChallengeProgress { Settings = settings, Window = window, Count = count, CoachPlan = coachPlan };
    }

    private async Task<int> CountAsync(Guid gymId, Guid traineeId, ChallengeWindow window, CancellationToken cancellationToken)
    {
        var done = await _completions.ListForTraineeAsync(gymId, traineeId, window.Start, cancellationToken);
        return window.Count(done.Select(c => new ChallengeWorkout(c.CompletedOn, c.IsVoided)));
    }
}
