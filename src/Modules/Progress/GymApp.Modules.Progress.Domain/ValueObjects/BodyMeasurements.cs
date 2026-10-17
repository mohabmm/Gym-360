using GymApp.Modules.Progress.Domain.Enums;

namespace GymApp.Modules.Progress.Domain.ValueObjects;

public sealed record BodyMeasurements
{
    public decimal? Chest { get; init; }
    public decimal? Waist { get; init; }
    public decimal? Hips { get; init; }
    public decimal? LeftArm { get; init; }
    public decimal? RightArm { get; init; }
    public decimal? LeftThigh { get; init; }
    public decimal? RightThigh { get; init; }
    public decimal? LeftCalf { get; init; }
    public decimal? RightCalf { get; init; }
    public decimal? Neck { get; init; }
    public MeasurementUnit MeasurementUnit { get; init; }
}
