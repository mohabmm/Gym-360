using GymApp.Common.Application.Messaging;
using GymApp.Modules.Nutrition.Application.DTOs;

namespace GymApp.Modules.Nutrition.Application.Commands.UpdateMealPlan;

public sealed record UpdateMealPlanCommand(
    Guid MealPlanId,
    string? Name,
    string? Description,
    int? DailyCalorieTarget,
    MacroTargetsDto? MacroTargets,
    List<MealPlanDayDto>? Days) : ICommand;
