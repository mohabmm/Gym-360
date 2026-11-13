namespace GymApp.Modules.Billing.Domain.Enums;

/// <summary>Where the trainee tapped "I'm interested" (pt.package_interest).</summary>
public enum PackageInterestSource
{
    /// <summary>On the coach's post-trial plan (pt.trial_recap).</summary>
    TrialRecap = 0,

    /// <summary>After a trial with no recap (trial-end or follow-up reminder).</summary>
    TrialFollowUp = 1,

    /// <summary>On a paid package that is low or used up.</summary>
    Renewal = 2,
}
