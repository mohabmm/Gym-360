namespace GymApp.Modules.Nutrition.Application.DTOs;

public sealed record FoodLogSummaryResponse(
    DateOnly From,
    DateOnly To,
    int TotalDays,
    decimal AverageCalories,
    decimal AverageProtein,
    decimal AverageCarbs,
    decimal AverageFat,
    List<DailyNutritionDto> DailyLogs);

public sealed record DailyNutritionDto(
    DateOnly Date,
    decimal TotalCalories,
    decimal TotalProtein,
    decimal TotalCarbs,
    decimal TotalFat);
