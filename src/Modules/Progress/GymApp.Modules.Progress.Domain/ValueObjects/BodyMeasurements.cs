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

    private BodyMeasurements() { }

    public static BodyMeasurements Create(
        MeasurementUnit unit,
        decimal? chest = null,
        decimal? waist = null,
        decimal? hips = null,
        decimal? leftArm = null,
        decimal? rightArm = null,
        decimal? leftThigh = null,
        decimal? rightThigh = null,
        decimal? leftCalf = null,
        decimal? rightCalf = null,
        decimal? neck = null)
    {
        return new BodyMeasurements
        {
            MeasurementUnit = unit,
            Chest = chest,
            Waist = waist,
            Hips = hips,
            LeftArm = leftArm,
            RightArm = rightArm,
            LeftThigh = leftThigh,
            RightThigh = rightThigh,
            LeftCalf = leftCalf,
            RightCalf = rightCalf,
            Neck = neck
        };
    }
}
