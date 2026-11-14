namespace GymApp.Modules.Progress.Domain.Enums;

public static class WeightUnitExtensions
{
    private const decimal KgPerPound = 0.45359237m;

    /// <summary>Converts a weight in this unit to kg.</summary>
    public static decimal? ToKg(this WeightUnit unit, decimal? weight) =>
        unit == WeightUnit.Lbs ? weight * KgPerPound : weight;
}
