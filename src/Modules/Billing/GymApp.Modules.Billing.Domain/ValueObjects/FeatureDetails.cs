namespace GymApp.Modules.Billing.Domain.ValueObjects;

/// <summary>What a platform feature is: its code, display name and catalogue placement.</summary>
public sealed record FeatureDetails
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public int SortOrder { get; init; }
}
