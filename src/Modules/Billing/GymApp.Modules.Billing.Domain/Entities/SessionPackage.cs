using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Events;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class SessionPackage : Entity
{
    private readonly List<PackageUsage> _usageHistory = new();

    private SessionPackage() { }

    public Guid GymId { get; private set; }
    public Guid TrainerId { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid TemplateId { get; private set; }
    public string Name { get; private set; } = default!;
    public int TotalSessions { get; private set; }
    public int RemainingSessions { get; private set; }
    public Money PricePerSession { get; private set; } = default!;
    public Money TotalPrice { get; private set; } = default!;
    public PackageStatus Status { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public IReadOnlyList<PackageUsage> UsageHistory => _usageHistory.AsReadOnly();
    public Guid AssignedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>Free trial package (pt.trial_sessions).</summary>
    public bool IsTrial { get; private set; }

    /// <summary>When the last session was used (trial end date for conversion tracking).</summary>
    public DateTime? ExhaustedAt { get; private set; }

    /// <summary>Bit set of <see cref="PackageMilestone"/> values already announced, so each fires once.</summary>
    public int MilestonesSent { get; private set; }

    /// <summary>Sessions counted against the package and not reversed.</summary>
    public int UsedSessions => TotalSessions - RemainingSessions;

    public static SessionPackage Create(SessionPackageDetails details)
    {
        return new SessionPackage
        {
            Id = Guid.NewGuid(),
            GymId = details.GymId,
            TrainerId = details.TrainerId,
            MemberId = details.MemberId,
            TemplateId = details.TemplateId,
            Name = details.Name,
            TotalSessions = details.TotalSessions,
            RemainingSessions = details.TotalSessions,
            PricePerSession = details.PricePerSession,
            TotalPrice = details.TotalPrice,
            Status = PackageStatus.Active,
            ExpiresAt = details.ExpiresAt,
            AssignedBy = details.AssignedBy,
            CreatedAt = DateTime.UtcNow,
            IsTrial = details.IsTrial
        };
    }

    public Result UseSession(Guid sessionId) => CountSession(sessionId, PackageUsageType.Completed);

    public Result RecordLateCancelUsage(Guid sessionId) => CountSession(sessionId, PackageUsageType.LateCancelled);

    /// <summary>
    /// Deducts one session for <paramref name="sessionId"/>. Idempotent per usage type: at-least-once
    /// outbox delivery may re-invoke this for a session already counted, and a repeat is a no-op
    /// success so the package is never double-deducted.
    /// </summary>
    private Result CountSession(Guid sessionId, PackageUsageType usageType)
    {
        if (_usageHistory.Any(u => u.SessionId == sessionId && u.UsageType == usageType))
            return Result.Success();

        if (Status == PackageStatus.Exhausted)
            return Result.Failure(Errors.BillingErrors.PackageExhausted);

        if (Status == PackageStatus.Expired)
            return Result.Failure(Errors.BillingErrors.PackageExpired);

        RemainingSessions--;
        _usageHistory.Add(PackageUsage.Create(Id, sessionId, usageType));

        CheckAndRaiseEvents();

        return Result.Success();
    }

    public void Expire()
    {
        Status = PackageStatus.Expired;
    }

    /// <summary>
    /// Gives back a session that was counted for <paramref name="sessionId"/> (the trainee said
    /// "not me" and the desk reversed it). Idempotent; never exceeds the package size.
    /// </summary>
    public Result RestoreSession(Guid sessionId)
    {
        var counted = _usageHistory.Count(u => u.SessionId == sessionId
            && u.UsageType is PackageUsageType.Completed or PackageUsageType.LateCancelled or PackageUsageType.NoShow);
        var reversed = _usageHistory.Count(u => u.SessionId == sessionId && u.UsageType == PackageUsageType.Reversed);

        if (counted <= reversed || RemainingSessions >= TotalSessions)
            return Result.Success();

        RemainingSessions++;
        _usageHistory.Add(PackageUsage.Create(Id, sessionId, PackageUsageType.Reversed));

        if (Status == PackageStatus.Exhausted)
        {
            Status = PackageStatus.Active;
            ExhaustedAt = null;
        }

        return Result.Success();
    }

    private void CheckAndRaiseEvents()
    {
        if (RemainingSessions <= 0)
        {
            RemainingSessions = 0;
            Status = PackageStatus.Exhausted;
            ExhaustedAt = DateTime.UtcNow;
            RaiseDomainEvent(new SessionPackageExhaustedDomainEvent(Id, MemberId));
        }
        else if (!IsTrial && RemainingSessions <= 3)
        {
            RaiseDomainEvent(new SessionPackageLowDomainEvent(Id, MemberId, RemainingSessions));
        }

        var milestone = MilestoneFor(IsTrial, RemainingSessions);
        if (milestone is { } m && MarkMilestone(m))
            RaiseDomainEvent(new SessionPackageMilestoneReachedDomainEvent(Id, GymId, MemberId, TrainerId, m, RemainingSessions));
    }

    /// <summary>
    /// Renewal reminders fire at exactly 3 and exactly 1 sessions left on paid packages; trials
    /// alert when one free session is left and when the trial is used up.
    /// </summary>
    public static PackageMilestone? MilestoneFor(bool isTrial, int remaining) => (isTrial, remaining) switch
    {
        (false, 3) => PackageMilestone.RenewalThreeLeft,
        (false, 1) => PackageMilestone.RenewalOneLeft,
        (true, 1) => PackageMilestone.TrialLastSession,
        (true, 0) => PackageMilestone.TrialFinished,
        _ => null,
    };

    private bool MarkMilestone(PackageMilestone milestone)
    {
        var bit = 1 << (int)milestone;
        if ((MilestonesSent & bit) != 0)
            return false;

        MilestonesSent |= bit;
        return true;
    }
}
