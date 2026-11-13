using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.IntegrationEvents;

/// <summary>
/// A package reached a point the trainee, coach or desk should hear about.
/// <see cref="Milestone"/> is one of <see cref="Milestones"/>.
/// </summary>
public sealed record SessionPackageMilestoneReachedIntegrationEvent(
    Guid PackageId,
    Guid GymId,
    Guid MemberId,
    Guid TrainerId,
    string Milestone,
    int Remaining,
    int TotalSessions,
    bool IsTrial) : IIntegrationEvent
{
    public static class Milestones
    {
        /// <summary>Paid package: exactly 3 sessions left (pt.renewal_reminders).</summary>
        public const string RenewalThreeLeft = "renewal_3_left";

        /// <summary>Paid package: exactly 1 session left (pt.renewal_reminders).</summary>
        public const string RenewalOneLeft = "renewal_1_left";

        /// <summary>Trial: next session is the last free one (pt.trial_end_alerts).</summary>
        public const string TrialLastSession = "trial_last_session";

        /// <summary>Trial: all free sessions used (pt.trial_end_alerts).</summary>
        public const string TrialFinished = "trial_finished";
    }
}
