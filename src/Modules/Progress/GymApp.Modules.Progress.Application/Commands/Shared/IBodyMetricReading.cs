using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Modules.Progress.Application.Commands.Shared;

/// <summary>The reading part of a body-metric command (self-logged or coach-measured).</summary>
public interface IBodyMetricReading
{
    DateOnly Date { get; }
    decimal? Weight { get; }
    string? WeightUnit { get; }
    decimal? BodyFatPercentage { get; }
    BodyMeasurementsDto? Measurements { get; }
    string? Notes { get; }
}
