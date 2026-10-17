using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.ValueObjects;

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

    public static PlanChangeLog Create(PlanChangeLogDetails details)
    {
        return new PlanChangeLog
        {
            Id = Guid.NewGuid(),
            GymId = details.GymId,
            ChangeType = details.ChangeType,
            EntityType = details.EntityType,
            EntityId = details.EntityId,
            OldValue = details.OldValue,
            NewValue = details.NewValue,
            Reason = details.Reason,
            ChangedBy = details.ChangedBy,
            ChangedAt = DateTime.UtcNow
        };
    }
}
