using GymApp.Common.Application.Messaging;
using GymApp.Modules.Nutrition.Application.DTOs;

namespace GymApp.Modules.Nutrition.Application.Commands.LogFood;

public sealed record LogFoodCommand(
    Guid UserId,
    Guid GymId,
    DateOnly Date,
    string MealType,
    List<FoodItemDto> FoodItems) : ICommand<Guid>;
