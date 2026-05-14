using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Domain.Entities;

namespace GymApp.Modules.Nutrition.Application.Mappers;

internal static class NutritionMapper
{
    public static FoodLogResponse ToFoodLogResponse(FoodLog foodLog) =>
        new(
            foodLog.Id,
            foodLog.UserId,
            foodLog.Date,
            foodLog.DailyTotals.TotalCalories,
            foodLog.DailyTotals.TotalProtein,
            foodLog.DailyTotals.TotalCarbs,
            foodLog.DailyTotals.TotalFat,
            foodLog.Meals.Select(ToMealEntryDto).ToList());

    public static MealEntryDto ToMealEntryDto(MealEntry meal) =>
        new(
            meal.Id,
            meal.MealType.ToString(),
            meal.TotalCalories,
            meal.TotalProtein,
            meal.TotalCarbs,
            meal.TotalFat,
            meal.FoodItems.Select(ToFoodItemDto).ToList());

    public static FoodItemDto ToFoodItemDto(Domain.ValueObjects.FoodItem item) =>
        new(item.Name, item.Quantity, item.Unit, item.Calories, item.Protein, item.Carbs, item.Fat);

    public static WaterLogResponse ToWaterLogResponse(WaterLog waterLog) =>
        new(
            waterLog.Id,
            waterLog.UserId,
            waterLog.Date,
            waterLog.TotalMl,
            waterLog.TargetMl,
            waterLog.Entries.Select(ToWaterEntryDto).ToList());

    public static WaterEntryDto ToWaterEntryDto(WaterEntry entry) =>
        new(entry.Id, entry.AmountMl, entry.LoggedAt);

    public static MealPlanDto ToMealPlanDto(MealPlan plan) =>
        new(
            plan.Id,
            plan.TrainerId,
            plan.ClientId,
            plan.Name,
            plan.Description,
            plan.DailyCalorieTarget,
            new MacroTargetsDto(
                plan.MacroTargets.ProteinGrams,
                plan.MacroTargets.CarbsGrams,
                plan.MacroTargets.FatGrams),
            plan.IsTemplate,
            plan.CreatedAt);

    public static MealPlanDetailDto ToMealPlanDetailDto(MealPlan plan) =>
        new(
            plan.Id,
            plan.TrainerId,
            plan.ClientId,
            plan.Name,
            plan.Description,
            plan.DailyCalorieTarget,
            new MacroTargetsDto(
                plan.MacroTargets.ProteinGrams,
                plan.MacroTargets.CarbsGrams,
                plan.MacroTargets.FatGrams),
            plan.IsTemplate,
            plan.CreatedAt,
            plan.UpdatedAt,
            plan.Days.Select(ToMealPlanDayDto).ToList());

    public static MealPlanDayDto ToMealPlanDayDto(MealPlanDay day) =>
        new(
            day.DayOfWeek,
            day.PlannedMeals.Select(ToPlannedMealDto).ToList(),
            day.Alternatives.Count > 0
                ? day.Alternatives.Select(ToPlannedMealDto).ToList()
                : null);

    public static PlannedMealDto ToPlannedMealDto(Domain.ValueObjects.PlannedMeal meal) =>
        new(
            meal.MealType.ToString(),
            meal.Name,
            meal.Description,
            meal.FoodItems.Select(ToFoodItemDto).ToList(),
            meal.TotalCalories,
            meal.TotalProtein,
            meal.TotalCarbs,
            meal.TotalFat);

    public static FoodDatabaseItemDto ToFoodDatabaseItemDto(FoodDatabase item) =>
        new(
            item.Id,
            item.Name,
            item.NameArabic,
            item.ServingSize,
            item.ServingUnit,
            item.CaloriesPerServing,
            item.ProteinPerServing,
            item.CarbsPerServing,
            item.FatPerServing,
            item.Category);
}
