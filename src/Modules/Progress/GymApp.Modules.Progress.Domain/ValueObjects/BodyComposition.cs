using GymApp.Common.Domain;

namespace GymApp.Modules.Progress.Domain.ValueObjects;

/// <summary>
/// Body-composition values read from an InBody result sheet: height, skeletal muscle mass and
/// total body water. Muscle and water percentages are derived from the entry's weight, not stored.
/// </summary>
public sealed record BodyComposition
{
    public const decimal MinHeightCm = 50m;
    public const decimal MaxHeightCm = 250m;

    public decimal? HeightCm { get; init; }
    public decimal? SkeletalMuscleMassKg { get; init; }
    public decimal? BodyWaterKg { get; init; }

    public bool IsEmpty => HeightCm is null && SkeletalMuscleMassKg is null && BodyWaterKg is null;

    /// <summary>Returns the first rule the values break, or null when they are valid.</summary>
    /// <param name="weightKg">The entry's weight in kg, if known; muscle and water must be lighter.</param>
    public Error? Validate(decimal? weightKg)
    {
        if (!IsValidHeight(HeightCm))
            return ProgressErrors.InvalidHeight;

        var massesValid = IsValidMass(SkeletalMuscleMassKg, weightKg) && IsValidMass(BodyWaterKg, weightKg);
        return massesValid ? null : ProgressErrors.InvalidBodyComposition;
    }

    private static bool IsValidHeight(decimal? heightCm) =>
        heightCm is null or (>= MinHeightCm and <= MaxHeightCm);

    /// <summary>Share of <paramref name="weightKg"/> made up by <paramref name="partKg"/>, to one decimal.</summary>
    public static decimal? PercentOf(decimal? partKg, decimal? weightKg) =>
        partKg is { } part && weightKg is > 0 ? Round1(part / weightKg.Value * 100m) : null;

    /// <summary>Body-mass index from weight (kg) and height (cm), to one decimal.</summary>
    public static decimal? Bmi(decimal? weightKg, decimal? heightCm)
    {
        if (weightKg is not > 0 || heightCm is not > 0)
            return null;

        var meters = heightCm.Value / 100m;
        return Round1(weightKg.Value / (meters * meters));
    }

    // As people round: 56.25 → 56.3 (not banker's rounding).
    private static decimal Round1(decimal value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static bool IsValidMass(decimal? massKg, decimal? weightKg) =>
        massKg is null || (massKg > 0 && (weightKg is null || massKg < weightKg));
}
