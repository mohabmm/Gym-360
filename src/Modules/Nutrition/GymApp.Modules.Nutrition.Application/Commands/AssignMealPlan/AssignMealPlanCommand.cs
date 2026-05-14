using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Nutrition.Application.Commands.AssignMealPlan;

public sealed record AssignMealPlanCommand(
    Guid MealPlanId,
    Guid TrainerId,
    Guid ClientId) : ICommand;
