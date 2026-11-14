namespace GymApp.Modules.Progress.Application.DTOs;

/// <summary>
/// InBody composition values. The percentages and BMI are derived from the entry's weight (kg)
/// and are null when weight or the underlying value is missing.
/// </summary>
public sealed record BodyCompositionDto(
    decimal? HeightCm,
    decimal? SkeletalMuscleMassKg,
    decimal? BodyWaterKg,
    decimal? SkeletalMusclePercent,
    decimal? BodyWaterPercent,
    decimal? Bmi);
