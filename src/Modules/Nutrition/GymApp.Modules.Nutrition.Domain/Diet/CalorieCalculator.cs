using GymApp.Common.Domain;

namespace GymApp.Modules.Nutrition.Domain.Diet;

/// <summary>What the coach enters (prefilled from the profile and the last measurement).</summary>
public sealed record BodyProfile(Sex Sex, int Age, decimal HeightCm, decimal WeightKg, ActivityLevel Activity, DietGoal Goal);

/// <summary>
/// A member's daily target: <see cref="Bmr"/> (at rest), <see cref="Maintenance"/> (with activity)
/// and <see cref="Calories"/> for the goal, split into protein, carbs and fat grams.
/// </summary>
public sealed record DailyTarget(int Bmr, int Maintenance, int Calories, int ProteinGrams, int CarbsGrams, int FatGrams);

/// <summary>
/// Mifflin-St Jeor BMR × activity, adjusted for the goal (−20% to lose fat, +10% to build
/// muscle) and never below a safe floor. Protein by body weight for the goal, fat a quarter
/// of the calories, carbs the rest.
/// </summary>
public static class CalorieCalculator
{
    public const int MinAge = 16;
    public const int MaxAge = 80;
    public const decimal MinHeightCm = 130;
    public const decimal MaxHeightCm = 220;
    public const decimal MinWeightKg = 35;
    public const decimal MaxWeightKg = 250;

    private const decimal FatShare = 0.25m;

    public static Result<DailyTarget> Calculate(BodyProfile body)
    {
        var problem = Problem(body);
        if (problem is not null)
            return Result.Failure<DailyTarget>(DietErrors.InvalidBody(problem));

        var bmr = 10m * body.WeightKg + 6.25m * body.HeightCm - 5m * body.Age + (body.Sex == Sex.Male ? 5m : -161m);
        var maintenance = bmr * ActivityFactor(body.Activity);
        var calories = Math.Max(maintenance * GoalFactor(body.Goal), Floor(body.Sex));
        return Result.Success(Split(RoundTo(bmr, 10), RoundTo(maintenance, 10), RoundTo(calories, 10), body));
    }

    /// <summary>Macros for a given calorie target (also used when the coach changes the calories).</summary>
    public static DailyTarget Split(int bmr, int maintenance, int calories, BodyProfile body)
    {
        var protein = RoundTo(body.WeightKg * ProteinPerKg(body.Goal), 5);
        var fat = RoundTo(calories * FatShare / 9m, 5);
        var carbs = Math.Max(0, RoundTo((calories - protein * 4m - fat * 9m) / 4m, 5));
        return new DailyTarget(bmr, maintenance, calories, protein, carbs, fat);
    }

    private static string? Problem(BodyProfile body)
    {
        if (body.Age is < MinAge or > MaxAge)
            return $"Age must be {MinAge} to {MaxAge}.";
        if (body.HeightCm is < MinHeightCm or > MaxHeightCm)
            return $"Height must be {MinHeightCm} to {MaxHeightCm} cm.";
        return body.WeightKg is < MinWeightKg or > MaxWeightKg ? $"Weight must be {MinWeightKg} to {MaxWeightKg} kg." : null;
    }

    private static decimal ActivityFactor(ActivityLevel level) => level switch
    {
        ActivityLevel.Low => 1.2m,
        ActivityLevel.Light => 1.375m,
        ActivityLevel.Moderate => 1.55m,
        _ => 1.725m
    };

    private static decimal GoalFactor(DietGoal goal) => goal switch
    {
        DietGoal.FatLoss => 0.8m,
        DietGoal.MuscleGain => 1.1m,
        _ => 1m
    };

    private static decimal ProteinPerKg(DietGoal goal) => goal switch
    {
        DietGoal.FatLoss => 2.0m,
        DietGoal.MuscleGain => 1.8m,
        _ => 1.6m
    };

    private static decimal Floor(Sex sex) => sex == Sex.Male ? 1500m : 1200m;

    private static int RoundTo(decimal value, int step) => (int)(Math.Round(value / step, MidpointRounding.AwayFromZero) * step);
}
