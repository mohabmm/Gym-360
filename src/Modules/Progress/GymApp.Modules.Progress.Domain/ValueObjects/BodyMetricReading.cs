using GymApp.Modules.Progress.Domain.Enums;

namespace GymApp.Modules.Progress.Domain.ValueObjects;

/// <summary>The measured values of a body-metric entry: weight, body fat, tape measurements, InBody composition and notes.</summary>
public sealed record BodyMetricReading
{
    public decimal? Weight { get; init; }
    public required WeightUnit WeightUnit { get; init; }
    public decimal? BodyFatPercentage { get; init; }
    public BodyMeasurements? Measurements { get; init; }
    public BodyComposition? Composition { get; init; }
    public string? Notes { get; init; }

    /// <summary>Weight converted to kg, for comparing against composition values.</summary>
    public decimal? WeightKg => WeightUnit.ToKg(Weight);
}
