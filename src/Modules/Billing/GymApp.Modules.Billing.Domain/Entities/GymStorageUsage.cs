using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class GymStorageUsage : Entity
{
    private GymStorageUsage() { }

    public Guid GymId { get; private set; }
    public string ResourceType { get; private set; } = default!;
    public long CurrentUsage { get; private set; }
    public DateTime LastCalculatedAt { get; private set; }

    public static GymStorageUsage Create(
        Guid gymId,
        string resourceType,
        long currentUsage = 0)
    {
        return new GymStorageUsage
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            ResourceType = resourceType,
            CurrentUsage = currentUsage,
            LastCalculatedAt = DateTime.UtcNow
        };
    }

    public void UpdateUsage(long currentUsage)
    {
        CurrentUsage = currentUsage;
        LastCalculatedAt = DateTime.UtcNow;
    }

    public void IncrementUsage(long amount)
    {
        CurrentUsage += amount;
        LastCalculatedAt = DateTime.UtcNow;
    }

    public void DecrementUsage(long amount)
    {
        CurrentUsage = Math.Max(0, CurrentUsage - amount);
        LastCalculatedAt = DateTime.UtcNow;
    }
}
