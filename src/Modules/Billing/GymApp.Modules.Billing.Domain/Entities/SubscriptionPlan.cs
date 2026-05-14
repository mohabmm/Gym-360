using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class SubscriptionPlan : Entity
{
    private SubscriptionPlan() { }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal MonthlyPriceEgp { get; private set; }
    public decimal AnnualPriceEgp { get; private set; }
    public int MemberLimit { get; private set; }
    public int TrainerLimit { get; private set; }
    public int StaffLimit { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsPublic { get; private set; }
    public bool IsTimeLimited { get; private set; }
    public DateTime? AvailableFrom { get; private set; }
    public DateTime? AvailableUntil { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static SubscriptionPlan Create(
        string code,
        string name,
        decimal monthlyPriceEgp,
        decimal annualPriceEgp,
        int memberLimit,
        int trainerLimit,
        int staffLimit,
        string? description = null,
        bool isPublic = true,
        bool isTimeLimited = false,
        DateTime? availableFrom = null,
        DateTime? availableUntil = null,
        int sortOrder = 0)
    {
        return new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            MonthlyPriceEgp = monthlyPriceEgp,
            AnnualPriceEgp = annualPriceEgp,
            MemberLimit = memberLimit,
            TrainerLimit = trainerLimit,
            StaffLimit = staffLimit,
            IsActive = true,
            IsPublic = isPublic,
            IsTimeLimited = isTimeLimited,
            AvailableFrom = availableFrom,
            AvailableUntil = availableUntil,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        string? description,
        decimal monthlyPriceEgp,
        decimal annualPriceEgp,
        int memberLimit,
        int trainerLimit,
        int staffLimit,
        bool isPublic,
        int sortOrder)
    {
        Name = name;
        Description = description;
        MonthlyPriceEgp = monthlyPriceEgp;
        AnnualPriceEgp = annualPriceEgp;
        MemberLimit = memberLimit;
        TrainerLimit = trainerLimit;
        StaffLimit = staffLimit;
        IsPublic = isPublic;
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
