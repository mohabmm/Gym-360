using GymApp.Modules.Progress.Domain.Enums;

namespace GymApp.Modules.Progress.Domain.ValueObjects;

/// <summary>Whose body metrics were recorded, on which day, by whom and how — plus the reading itself.</summary>
public sealed record BodyMetricEntryDetails
{
    public required Guid UserId { get; init; }
    public required Guid GymId { get; init; }
    public required DateOnly Date { get; init; }
    public required MetricSource Source { get; init; }
    public required Guid LoggedBy { get; init; }
    public required BodyMetricReading Reading { get; init; }
}
