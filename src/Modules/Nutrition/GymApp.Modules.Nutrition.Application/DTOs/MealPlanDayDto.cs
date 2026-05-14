namespace GymApp.Modules.Nutrition.Application.DTOs;

public sealed record MealPlanDayDto(
    DayOfWeek DayOfWeek,
    List<PlannedMealDto> PlannedMeals,
    List<PlannedMealDto>? Alternatives = null);
