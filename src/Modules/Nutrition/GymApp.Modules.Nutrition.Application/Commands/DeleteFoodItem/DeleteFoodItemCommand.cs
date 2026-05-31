using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Nutrition.Application.Commands.DeleteFoodItem;

/// <summary>
/// Deletes a single food item from a logged meal. Food items have no stable id
/// (they are JSON value objects), so they are addressed by their position in the
/// meal — <see cref="ItemIndex"/> matches the order returned by the food-log GET.
/// </summary>
public sealed record DeleteFoodItemCommand(
    Guid UserId,
    Guid MealEntryId,
    int ItemIndex) : ICommand;
