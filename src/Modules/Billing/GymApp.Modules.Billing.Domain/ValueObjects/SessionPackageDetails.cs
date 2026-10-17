namespace GymApp.Modules.Billing.Domain.ValueObjects;

/// <summary>A PT session package assigned to a member: trainer, size, pricing and expiry.</summary>
public sealed record SessionPackageDetails
{
    public required Guid GymId { get; init; }
    public required Guid TrainerId { get; init; }
    public required Guid MemberId { get; init; }
    public required Guid TemplateId { get; init; }
    public required string Name { get; init; }
    public required int TotalSessions { get; init; }
    public required Money PricePerSession { get; init; }
    public required Money TotalPrice { get; init; }
    public required Guid AssignedBy { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
