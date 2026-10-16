namespace GymApp.Modules.Billing.Domain.ValueObjects;

/// <summary>One audited plan/feature change: what changed, on which entity, from/to and why.</summary>
public sealed record PlanChangeLogDetails
{
    public required string ChangeType { get; init; }
    public required Guid ChangedBy { get; init; }
    public Guid? GymId { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string? Reason { get; init; }
}
