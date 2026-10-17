namespace GymApp.Modules.Nutrition.Domain.ValueObjects;

/// <summary>A food reference entry: its names, category, serving and per-serving macros.</summary>
public sealed record FoodDatabaseDetails
{
    public required string Name { get; init; }
    public required string NameArabic { get; init; }
    public required decimal ServingSize { get; init; }
    public required string ServingUnit { get; init; }
    public required decimal CaloriesPerServing { get; init; }
    public required decimal ProteinPerServing { get; init; }
    public required decimal CarbsPerServing { get; init; }
    public required decimal FatPerServing { get; init; }
    public required string Category { get; init; }
}
