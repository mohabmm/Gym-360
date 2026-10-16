using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class MembershipPlan : Entity
{
    private readonly List<string> _features = new();

    private MembershipPlan() { }

    public Guid GymId { get; private set; }
    public string Name { get; private set; } = default!;
    public int DurationDays { get; private set; }
    public Money Price { get; private set; } = default!;
    public IReadOnlyList<string> Features => _features.AsReadOnly();
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static MembershipPlan Create(MembershipPlanDetails details)
    {
        var plan = new MembershipPlan
        {
            Id = Guid.NewGuid(),
            GymId = details.GymId,
            Name = details.Name,
            DurationDays = details.DurationDays,
            Price = details.Price,
            IsActive = true,
            SortOrder = details.SortOrder,
            CreatedAt = DateTime.UtcNow
        };

        if (details.Features is not null)
            plan._features.AddRange(details.Features);

        return plan;
    }

    public void Update(string name, int durationDays, Money price, int sortOrder)
    {
        Name = name;
        DurationDays = durationDays;
        Price = price;
        SortOrder = sortOrder;
    }

    public void SetFeatures(List<string> features)
    {
        _features.Clear();
        _features.AddRange(features);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
