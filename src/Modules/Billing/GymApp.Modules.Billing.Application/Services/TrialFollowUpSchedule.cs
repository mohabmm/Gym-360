using GymApp.Modules.Billing.Domain.Events;

namespace GymApp.Modules.Billing.Application.Services;

/// <summary>
/// pt.trial_followups: which post-trial reminder is due. Day 3 from 3 to 11 days after the trial
/// finished, the last-chance reminder from day 12 until the 14-day conversion window closes.
/// A trainee reached late (e.g. the job was down) only gets the reminder for their current age.
/// </summary>
public static class TrialFollowUpSchedule
{
    public const int Day3 = 3;
    public const int LastChanceDay = 12;

    public static readonly TimeSpan LookBack = PtMetricsCalculator.ConversionWindow;

    public static PackageMilestone? DueOn(DateTime trialFinishedAtUtc, DateTime nowUtc)
    {
        var age = nowUtc - trialFinishedAtUtc;
        if (age < TimeSpan.FromDays(Day3) || age >= LookBack)
            return null;

        return age < TimeSpan.FromDays(LastChanceDay)
            ? PackageMilestone.TrialFollowUpDay3
            : PackageMilestone.TrialLastChanceDay12;
    }
}
