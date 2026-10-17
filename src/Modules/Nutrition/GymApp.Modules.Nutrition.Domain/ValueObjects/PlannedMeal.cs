using GymApp.Modules.Nutrition.Domain.Enums;

namespace GymApp.Modules.Nutrition.Domain.ValueObjects;

public sealed record PlannedMeal
{
    // Public parameterless constructor (via init-only properties) is also used by
    // EF Core JSON deserialization, so copy-constructor detection never kicks in.
    public MealType MealType { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyList<FoodItem> FoodItems { get; init; } = Array.Empty<FoodItem>();
    public decimal TotalCalories { get; init; }
    public decimal TotalProtein { get; init; }
    public decimal TotalCarbs { get; init; }
    public decimal TotalFat { get; init; }
}
