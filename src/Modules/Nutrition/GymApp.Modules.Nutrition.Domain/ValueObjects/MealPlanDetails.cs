namespace GymApp.Modules.Nutrition.Domain.ValueObjects;

/// <summary>What a meal plan is: its name, description and daily calorie/macro targets.</summary>
public sealed record MealPlanDetails
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required decimal DailyCalorieTarget { get; init; }
    public required MacroTargets MacroTargets { get; init; }
}
