namespace GymApp.Modules.Nutrition.Application.DTOs;

public sealed record MealEntryDto(
    Guid Id,
    string MealType,
    decimal TotalCalories,
    decimal TotalProtein,
    decimal TotalCarbs,
    decimal TotalFat,
    List<FoodItemDto> FoodItems);
