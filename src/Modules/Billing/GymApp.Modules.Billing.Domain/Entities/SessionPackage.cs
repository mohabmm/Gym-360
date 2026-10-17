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
            CreatedAt = DateTime.UtcNow
        };
    }

    public Result UseSession(Guid sessionId)
    {
        // Idempotency: at-least-once outbox delivery may re-invoke this for a session already
        // counted. Treat a repeat as a no-op success so the package is never double-deducted.
        if (_usageHistory.Any(u => u.SessionId == sessionId && u.UsageType == PackageUsageType.Completed))
            return Result.Success();

        if (Status == PackageStatus.Exhausted)
            return Result.Failure(Errors.BillingErrors.PackageExhausted);

        if (Status == PackageStatus.Expired)
            return Result.Failure(Errors.BillingErrors.PackageExpired);

        RemainingSessions--;
        _usageHistory.Add(PackageUsage.Create(Id, sessionId, PackageUsageType.Completed));

        CheckAndRaiseEvents();

        return Result.Success();
    }

    public Result RecordLateCancelUsage(Guid sessionId)
    {
        // Idempotency: see UseSession — a repeated late-cancel for the same session is a no-op.
        if (_usageHistory.Any(u => u.SessionId == sessionId && u.UsageType == PackageUsageType.LateCancelled))
            return Result.Success();

        if (Status == PackageStatus.Exhausted)
            return Result.Failure(Errors.BillingErrors.PackageExhausted);

        if (Status == PackageStatus.Expired)
            return Result.Failure(Errors.BillingErrors.PackageExpired);

        RemainingSessions--;
        _usageHistory.Add(PackageUsage.Create(Id, sessionId, PackageUsageType.LateCancelled));

        CheckAndRaiseEvents();

        return Result.Success();
    }

    public void Expire()
    {
        Status = PackageStatus.Expired;
    }

    private void CheckAndRaiseEvents()
    {
        if (RemainingSessions <= 0)
        {
            RemainingSessions = 0;
            Status = PackageStatus.Exhausted;
            RaiseDomainEvent(new SessionPackageExhaustedDomainEvent(Id, MemberId));
        }
        else if (RemainingSessions <= 3)
        {
            RaiseDomainEvent(new SessionPackageLowDomainEvent(Id, MemberId, RemainingSessions));
        }
    }
}
