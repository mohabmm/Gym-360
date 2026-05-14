namespace GymApp.Modules.Nutrition.Application.DTOs;

public sealed record FoodItemDto(
    string Name,
    decimal Quantity,
    string Unit,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat);
