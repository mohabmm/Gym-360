namespace GymApp.Modules.Billing.Domain.ValueObjects;

/// <summary>Which plan a gym subscribes to, and whether it starts as a (time-boxed) trial.</summary>
public sealed record GymSubscriptionDetails
{
    public required Guid GymId { get; init; }
    public required Guid PlanId { get; init; }
    public bool IsTrial { get; init; }
    public DateTime? TrialExpiresAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
