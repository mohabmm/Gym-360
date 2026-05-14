namespace GymApp.Modules.Nutrition.Domain.ValueObjects;

public sealed record FoodItem(
    string Name,
    decimal Quantity,
    string Unit,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat)
{
    // Private parameterless constructor required for EF Core JSON deserialization.
    private FoodItem() : this(string.Empty, 0, string.Empty, 0, 0, 0, 0) { }
}
