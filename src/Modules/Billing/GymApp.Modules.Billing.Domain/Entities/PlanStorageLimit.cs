using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class PlanStorageLimit : Entity
{
    private PlanStorageLimit() { }

    public Guid PlanId { get; private set; }
    public string ResourceType { get; private set; } = default!;
    public long LimitValue { get; private set; }

    public static PlanStorageLimit Create(
        Guid planId,
        string resourceType,
        long limitValue)
    {
        return new PlanStorageLimit
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            ResourceType = resourceType,
            LimitValue = limitValue
        };
    }

    public void Update(long limitValue)
    {
        LimitValue = limitValue;
    }

    public bool IsUnlimited => LimitValue == -1;
}
