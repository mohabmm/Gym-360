namespace GymApp.Modules.Billing.Domain.ValueObjects;

/// <summary>
/// What a platform subscription plan offers: names, pricing, seat limits and visibility.
/// <see cref="Code"/> and the availability window are only applied on creation.
/// </summary>
public sealed record SubscriptionPlanDetails
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required decimal MonthlyPriceEgp { get; init; }
    public required decimal AnnualPriceEgp { get; init; }
    public required int MemberLimit { get; init; }
    public required int TrainerLimit { get; init; }
    public required int StaffLimit { get; init; }
    public bool IsPublic { get; init; } = true;
    public bool IsTimeLimited { get; init; }
    public DateTime? AvailableFrom { get; init; }
    public DateTime? AvailableUntil { get; init; }
    public int SortOrder { get; init; }
}
