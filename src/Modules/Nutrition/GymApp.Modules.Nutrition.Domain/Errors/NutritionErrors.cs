using GymApp.Common.Domain;

namespace GymApp.Modules.Nutrition.Domain.Errors;

public static class NutritionErrors
{
    public static Error FoodLogNotFound(Guid id) => new("Nutrition.FoodLogNotFound", $"Food log with ID '{id}' was not found.");

    public static Error WaterLogNotFound(Guid id) => new("Nutrition.WaterLogNotFound", $"Water log with ID '{id}' was not found.");

    public static Error MealPlanNotFound(Guid id) => new("Nutrition.MealPlanNotFound", $"Meal plan with ID '{id}' was not found.");

    public static Error FoodItemNotFound(Guid mealId, int index) => new("Nutrition.FoodItemNotFound", $"No food item at index {index} in meal '{mealId}'.");

    public static readonly Error DuplicateFoodLog = new("Nutrition.DuplicateFoodLog", "A food log already exists for this user on this date.");

    public static readonly Error InvalidCalories = new("Nutrition.InvalidCalories", "Calorie value must be zero or positive.");

    public static readonly Error InvalidWaterAmount = new("Nutrition.InvalidWaterAmount", "Water amount must be greater than zero.");

    public static readonly Error MealPlanAlreadyAssigned = new("Nutrition.MealPlanAlreadyAssigned", "This meal plan has already been assigned to a client.");
}
