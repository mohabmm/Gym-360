namespace GymApp.Modules.Nutrition.Application.DTOs;

public sealed record FoodDatabaseItemDto(
    Guid Id,
    string Name,
    string NameArabic,
    decimal ServingSize,
    string ServingUnit,
    decimal CaloriesPerServing,
    decimal ProteinPerServing,
    decimal CarbsPerServing,
    decimal FatPerServing,
    string Category);
