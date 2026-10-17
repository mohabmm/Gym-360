namespace GymApp.Modules.Billing.Domain.ValueObjects;

/// <summary>What a trainer earned for one completed session, and for whom.</summary>
public sealed record TrainerEarningsDetails
{
    public required Guid GymId { get; init; }
    public required Guid TrainerId { get; init; }
    public required Guid SessionId { get; init; }
    public required Guid MemberId { get; init; }
    public required decimal RateApplied { get; init; }
}
