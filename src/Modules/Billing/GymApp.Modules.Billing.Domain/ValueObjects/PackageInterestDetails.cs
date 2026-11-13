using GymApp.Modules.Billing.Domain.Enums;

namespace GymApp.Modules.Billing.Domain.ValueObjects;

/// <summary>A trainee's "I'm interested" tap on one of their packages.</summary>
public sealed record PackageInterestDetails
{
    public required Guid GymId { get; init; }
    public required Guid MemberId { get; init; }
    public required Guid TrainerId { get; init; }
    public required Guid SourcePackageId { get; init; }
    public required PackageInterestSource Source { get; init; }
    public int? RecommendedSessions { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}
