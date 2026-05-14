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

    public static SessionPackage Create(
        Guid gymId,
        Guid trainerId,
        Guid memberId,
        Guid templateId,
        string name,
        int totalSessions,
        Money pricePerSession,
        Money totalPrice,
        Guid assignedBy,
        DateTime? expiresAt = null)
    {
        return new SessionPackage
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            TrainerId = trainerId,
            MemberId = memberId,
            TemplateId = templateId,
            Name = name,
            TotalSessions = totalSessions,
            RemainingSessions = totalSessions,
            PricePerSession = pricePerSession,
            TotalPrice = totalPrice,
            Status = PackageStatus.Active,
            ExpiresAt = expiresAt,
            AssignedBy = assignedBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Result UseSession(Guid sessionId)
    {
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
