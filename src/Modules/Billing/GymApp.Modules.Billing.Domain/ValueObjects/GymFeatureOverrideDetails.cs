namespace GymApp.Modules.Billing.Domain.ValueObjects;

/// <summary>A per-gym feature override: which feature, the forced value, and who set it and why.</summary>
public sealed record GymFeatureOverrideDetails
{
    public required Guid GymId { get; init; }
    public required Guid FeatureId { get; init; }
    public required bool OverrideValue { get; init; }
    public required Guid SetBy { get; init; }
    public string? Reason { get; init; }
}
