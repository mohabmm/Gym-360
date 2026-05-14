using GymApp.Common.Application.Messaging;
using GymApp.Modules.Nutrition.Application.DTOs;

namespace GymApp.Modules.Nutrition.Application.Commands.CreateMealPlan;

public sealed record CreateMealPlanCommand(
    Guid TrainerId,
    Guid GymId,
    string Name,
    string? Description,
    int DailyCalorieTarget,
    MacroTargetsDto MacroTargets,
    List<MealPlanDayDto> Days,
    bool IsTemplate) : ICommand<Guid>;
