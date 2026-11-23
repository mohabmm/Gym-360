using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Errors;

namespace GymApp.Modules.Training.Domain.Entities;

/// <summary>
/// pt.training_plan: one row per gym, set by the owner or a manager. A gym without a row uses
/// the defaults (<see cref="ForGym"/>); the row is created on the first change.
/// </summary>
public sealed class TrainingPlanSettings : Entity
{
    public const int DefaultInactiveDays = 5;
    public const int MinInactiveDays = 2;
    public const int MaxInactiveDays = 30;

    // mem.first_month_challenge
    public const int DefaultChallengeTarget = 12;
    public const int MinChallengeTarget = 1;
    public const int MaxChallengeTarget = 60;
    public const int DefaultChallengeWindowDays = 30;
    public const int MinChallengeWindowDays = 7;
    public const int MaxChallengeWindowDays = 90;
    public const string DefaultRewardTextAr = "جلسة متابعة مع المدرب: قياسات جديدة وجدول الشهر الجاي";
    public const string DefaultRewardTextEn = "A follow-up session with your coach: new measurements and next month's plan";

    private TrainingPlanSettings() { }

    private TrainingPlanSettings(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    /// <summary>A trainee with a plan and no workout for this many days shows as "needs a nudge".</summary>
    public int InactiveDays { get; private set; } = DefaultInactiveDays;
    /// <summary>Plan workouts a new member must finish inside the window.</summary>
    public int ChallengeTarget { get; private set; } = DefaultChallengeTarget;
    /// <summary>Days from the join date (or the first plan's start) that count.</summary>
    public int ChallengeWindowDays { get; private set; } = DefaultChallengeWindowDays;
    /// <summary>The gym's own reward wording; null = the default text.</summary>
    public string? RewardTextAr { get; private set; }
    public string? RewardTextEn { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public static TrainingPlanSettings ForGym(Guid gymId) => new(Guid.NewGuid()) { GymId = gymId, UpdatedAt = DateTime.UtcNow };

    public Result SetInactiveDays(int days, Guid updatedBy)
    {
        if (days is < MinInactiveDays or > MaxInactiveDays)
            return Result.Failure(TrainingPlanErrors.InvalidSettings($"Inactive days must be {MinInactiveDays} to {MaxInactiveDays}."));
        InactiveDays = days;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public string EffectiveRewardTextAr => RewardTextAr ?? DefaultRewardTextAr;

    /// <summary>English falls back to the Arabic when the gym wrote only Arabic, else to the default.</summary>
    public string EffectiveRewardTextEn => RewardTextEn ?? (RewardTextAr is null ? DefaultRewardTextEn : RewardTextAr);

    /// <summary>Blank reward texts go back to the default wording.</summary>
    public Result SetChallenge(ChallengeSettings challenge, Guid updatedBy)
    {
        var invalid = ChallengeSettingsRules.Check(challenge);
        if (invalid is not null)
            return Result.Failure(TrainingPlanErrors.InvalidSettings(invalid));
        ChallengeTarget = challenge.Target;
        ChallengeWindowDays = challenge.WindowDays;
        RewardTextAr = Clean(challenge.RewardTextAr);
        RewardTextEn = Clean(challenge.RewardTextEn);
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}

/// <summary>mem.first_month_challenge settings as the owner or a manager sends them.</summary>
public sealed record ChallengeSettings(int Target, int WindowDays, string? RewardTextAr, string? RewardTextEn);

internal static class ChallengeSettingsRules
{
    /// <summary>Why the settings are refused, or null when they are fine.</summary>
    public static string? Check(ChallengeSettings c)
    {
        if (c.Target is < TrainingPlanSettings.MinChallengeTarget or > TrainingPlanSettings.MaxChallengeTarget)
            return $"The target must be {TrainingPlanSettings.MinChallengeTarget} to {TrainingPlanSettings.MaxChallengeTarget} workouts.";
        if (c.WindowDays is < TrainingPlanSettings.MinChallengeWindowDays or > TrainingPlanSettings.MaxChallengeWindowDays)
            return $"The window must be {TrainingPlanSettings.MinChallengeWindowDays} to {TrainingPlanSettings.MaxChallengeWindowDays} days.";
        var tooLong = new[] { c.RewardTextAr, c.RewardTextEn }.Any(t => t?.Trim().Length > ChallengeAward.MaxRewardTextLength);
        return tooLong ? $"The reward text is at most {ChallengeAward.MaxRewardTextLength} characters." : null;
    }
}
