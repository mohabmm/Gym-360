namespace GymApp.Modules.Nutrition.Application.DTOs;

public sealed record FoodLogResponse(
    Guid Id,
    Guid UserId,
    DateOnly Date,
    decimal TotalCalories,
    decimal TotalProtein,
    decimal TotalCarbs,
    decimal TotalFat,
    List<MealEntryDto> Meals);
