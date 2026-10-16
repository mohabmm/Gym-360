namespace GymApp.Modules.Billing.Domain.ValueObjects;

/// <summary>What a gym membership plan offers: name, duration, price and perks.</summary>
public sealed record MembershipPlanDetails
{
    public required Guid GymId { get; init; }
    public required string Name { get; init; }
    public required int DurationDays { get; init; }
    public required Money Price { get; init; }
    public List<string>? Features { get; init; }
    public int SortOrder { get; init; }
}
