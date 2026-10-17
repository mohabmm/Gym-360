namespace GymApp.Modules.Nutrition.Domain.ValueObjects;

public sealed record FoodItem
{
    // Public parameterless constructor (via init-only properties) is also used by
    // EF Core JSON deserialization.
    public string Name { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public decimal Calories { get; init; }
    public decimal Protein { get; init; }
    public decimal Carbs { get; init; }
    public decimal Fat { get; init; }
}
