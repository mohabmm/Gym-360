using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class PlanChangeLog : Entity
{
    private PlanChangeLog() { }

    public Guid? GymId { get; private set; }
    public string ChangeType { get; private set; } = default!;
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Reason { get; private set; }
    public Guid ChangedBy { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public static PlanChangeLog Create(
        string changeType,
        Guid changedBy,
        Guid? gymId = null,
        string? entityType = null,
        Guid? entityId = null,
        string? oldValue = null,
        string? newValue = null,
        string? reason = null)
    {
        return new PlanChangeLog
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            ChangeType = changeType,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            Reason = reason,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow
        };
    }
}
