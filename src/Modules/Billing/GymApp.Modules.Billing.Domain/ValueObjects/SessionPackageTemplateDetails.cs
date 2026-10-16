namespace GymApp.Modules.Billing.Domain.ValueObjects;

/// <summary>A reusable session package offer: name, session count and pricing.</summary>
public sealed record SessionPackageTemplateDetails
{
    public required Guid GymId { get; init; }
    public required string Name { get; init; }
    public required int SessionCount { get; init; }
    public required Money PricePerSession { get; init; }
    public required Money TotalPrice { get; init; }
}
