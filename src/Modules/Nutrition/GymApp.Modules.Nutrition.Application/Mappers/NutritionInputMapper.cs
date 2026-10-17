using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Enums;
using GymApp.Modules.Nutrition.Domain.ValueObjects;

namespace GymApp.Modules.Nutrition.Application.Mappers;

/// <summary>Maps incoming nutrition DTOs (request payloads) to domain value objects and entities.</summary>
internal static class NutritionInputMapper
{
    public static MacroTargets ToMacroTargets(MacroTargetsDto dto) =>
        new(dto.ProteinGrams, dto.CarbsGrams, dto.FatGrams);

    public static MealPlanDay ToMealPlanDay(Guid mealPlanId, MealPlanDayDto dto)
    {
        var plannedMeals = ToPlannedMeals(dto.PlannedMeals);
        var alternatives = dto.Alternatives is not null
            ? ToPlannedMeals(dto.Alternatives)
            : null;

        return MealPlanDay.Create(mealPlanId, dto.DayOfWeek, plannedMeals, alternatives);
    }

    public static List<PlannedMeal> ToPlannedMeals(List<PlannedMealDto> dtos) =>
        dtos.Select(ToPlannedMeal).ToList();

    public static PlannedMeal ToPlannedMeal(PlannedMealDto dto) =>
        new()
        {
            MealType = Enum.Parse<MealType>(dto.MealType, ignoreCase: true),
            Name = dto.Name,
            Description = dto.Description,
            FoodItems = dto.FoodItems.Select(ToFoodItem).ToList(),
            TotalCalories = dto.TotalCalories,
            TotalProtein = dto.TotalProtein,
            TotalCarbs = dto.TotalCarbs,
            TotalFat = dto.TotalFat
        };

    public static FoodItem ToFoodItem(FoodItemDto dto) =>
        new()
        {
            Name = dto.Name,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            Calories = dto.Calories,
            Protein = dto.Protein,
            Carbs = dto.Carbs,
            Fat = dto.Fat
        };
}
