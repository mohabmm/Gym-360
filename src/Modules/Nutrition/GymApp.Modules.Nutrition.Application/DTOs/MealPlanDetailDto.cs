namespace GymApp.Modules.Nutrition.Application.DTOs;

public sealed record MealPlanDetailDto(
    Guid Id,
    Guid TrainerId,
    Guid? ClientId,
    string Name,
    string? Description,
    decimal DailyCalorieTarget,
    MacroTargetsDto MacroTargets,
    bool IsTemplate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<MealPlanDayDto> Days);
