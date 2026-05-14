namespace GymApp.Modules.Nutrition.Domain.ValueObjects;

public sealed record NutritionTotals(
    decimal TotalCalories,
    decimal TotalProtein,
    decimal TotalCarbs,
    decimal TotalFat)
{
    public static NutritionTotals Zero => new(0, 0, 0, 0);

    public static NutritionTotals operator +(NutritionTotals a, NutritionTotals b) =>
        new(
            a.TotalCalories + b.TotalCalories,
            a.TotalProtein + b.TotalProtein,
            a.TotalCarbs + b.TotalCarbs,
            a.TotalFat + b.TotalFat);
}
