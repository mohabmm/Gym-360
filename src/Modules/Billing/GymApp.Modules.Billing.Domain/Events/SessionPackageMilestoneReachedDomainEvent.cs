using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Events;

/// <summary>A point in a package's life that the trainee, coach or desk should hear about.</summary>
public enum PackageMilestone
{
    /// <summary>Paid package: exactly 3 sessions left.</summary>
    RenewalThreeLeft = 0,

    /// <summary>Paid package: exactly 1 session left.</summary>
    RenewalOneLeft = 1,

    /// <summary>Trial: the next session is the last free one.</summary>
    TrialLastSession = 2,

    /// <summary>Trial: all free sessions used.</summary>
    TrialFinished = 3,
}

public sealed record SessionPackageMilestoneReachedDomainEvent(
    Guid PackageId,
    Guid GymId,
    Guid MemberId,
    Guid TrainerId,
    PackageMilestone Milestone,
    int Remaining) : IDomainEvent;
