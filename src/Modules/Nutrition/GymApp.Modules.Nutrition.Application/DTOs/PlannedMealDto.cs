namespace GymApp.Modules.Nutrition.Application.DTOs;

public sealed record PlannedMealDto(
    string MealType,
    string Name,
    string? Description,
    List<FoodItemDto> FoodItems,
    decimal TotalCalories,
    decimal TotalProtein,
    decimal TotalCarbs,
    decimal TotalFat);
