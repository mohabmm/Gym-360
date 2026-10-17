using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.ValueObjects;

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

    public static SubscriptionPlan Create(SubscriptionPlanDetails details)
    {
        return new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = details.Code,
            Name = details.Name,
            Description = details.Description,
            MonthlyPriceEgp = details.MonthlyPriceEgp,
            AnnualPriceEgp = details.AnnualPriceEgp,
            MemberLimit = details.MemberLimit,
            TrainerLimit = details.TrainerLimit,
            StaffLimit = details.StaffLimit,
            IsActive = true,
            IsPublic = details.IsPublic,
            IsTimeLimited = details.IsTimeLimited,
            AvailableFrom = details.AvailableFrom,
            AvailableUntil = details.AvailableUntil,
            SortOrder = details.SortOrder,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Applies editable catalogue fields. <see cref="Code"/> and the availability window
    /// (<see cref="IsTimeLimited"/>, <see cref="AvailableFrom"/>, <see cref="AvailableUntil"/>) are immutable after creation.
    /// </summary>
    public void Update(SubscriptionPlanDetails details)
    {
        Name = details.Name;
        Description = details.Description;
        MonthlyPriceEgp = details.MonthlyPriceEgp;
        AnnualPriceEgp = details.AnnualPriceEgp;
        MemberLimit = details.MemberLimit;
        TrainerLimit = details.TrainerLimit;
        StaffLimit = details.StaffLimit;
        IsPublic = details.IsPublic;
        SortOrder = details.SortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
