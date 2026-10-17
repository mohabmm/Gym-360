namespace GymApp.Modules.Gamification.Domain.ValueObjects;

/// <summary>A client's star rating (and optional comment) of a trainer for one session.</summary>
public sealed record TrainerRatingDetails
{
    public required Guid GymId { get; init; }
    public required Guid TrainerId { get; init; }
    public required Guid ClientId { get; init; }
    public required Guid SessionId { get; init; }
    public required int Stars { get; init; }
    public string? Comment { get; init; }
}
