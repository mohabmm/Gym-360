using GymApp.Modules.Progress.Domain.Enums;

namespace GymApp.Modules.Progress.Domain.ValueObjects;

/// <summary>The measured values of a body-metric entry: weight, body fat, tape measurements and notes.</summary>
public sealed record BodyMetricReading
{
    public decimal? Weight { get; init; }
    public required WeightUnit WeightUnit { get; init; }
    public decimal? BodyFatPercentage { get; init; }
    public BodyMeasurements? Measurements { get; init; }
    public string? Notes { get; init; }
}
