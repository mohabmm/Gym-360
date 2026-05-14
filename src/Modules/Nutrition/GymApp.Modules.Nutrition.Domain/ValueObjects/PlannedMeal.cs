using GymApp.Modules.Nutrition.Domain.Enums;

namespace GymApp.Modules.Nutrition.Domain.ValueObjects;

public sealed record PlannedMeal(
    MealType MealType,
    string Name,
    string? Description,
    IReadOnlyList<FoodItem> FoodItems,
    decimal TotalCalories,
    decimal TotalProtein,
    decimal TotalCarbs,
    decimal TotalFat)
{
    // Private parameterless constructor required for EF Core JSON deserialization.
    // EF Core's copy-constructor detection (PlannedMeal original) is bypassed
    // when a parameterless constructor is present.
    private PlannedMeal() : this(
        default,
        string.Empty,
        null,
        Array.Empty<FoodItem>(),
        0, 0, 0, 0) { }
}
