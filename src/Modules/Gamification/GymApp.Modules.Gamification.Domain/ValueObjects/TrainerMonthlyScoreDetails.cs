namespace GymApp.Modules.Gamification.Domain.ValueObjects;

/// <summary>A trainer's aggregated rating figures for one calendar month.</summary>
public sealed record TrainerMonthlyScoreDetails
{
    public required Guid GymId { get; init; }
    public required Guid TrainerId { get; init; }
    public required int Month { get; init; }
    public required int Year { get; init; }
    public required decimal AggregatedScore { get; init; }
    public required int TotalRatings { get; init; }
    public required int UniqueClients { get; init; }
    public required int SessionCount { get; init; }
}
