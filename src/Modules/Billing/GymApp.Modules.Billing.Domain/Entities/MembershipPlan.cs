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

    public static MembershipPlan Create(
        Guid gymId,
        string name,
        int durationDays,
        Money price,
        List<string>? features = null,
        int sortOrder = 0)
    {
        var plan = new MembershipPlan
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Name = name,
            DurationDays = durationDays,
            Price = price,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow
        };

        if (features is not null)
            plan._features.AddRange(features);

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
