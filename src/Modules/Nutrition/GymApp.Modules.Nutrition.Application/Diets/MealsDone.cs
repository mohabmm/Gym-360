using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.ValueObjects;

namespace GymApp.Modules.Nutrition.Application.Diets;

/// <summary>
/// A planned meal counts as eaten when the day's food log has an entry of the same meal type with
/// the same foods (the plan's meal or its alternative) — what "أكلتها ✓" logs. Matching on foods,
/// not only the meal type, tells the two snacks of a day apart.
/// </summary>
internal static class MealsDone
{
    public static bool IsDone(FoodLog? log, PlannedMeal meal, PlannedMeal? alternative) =>
        log is not null && log.Meals.Any(entry => Matches(entry, meal) || (alternative is not null && Matches(entry, alternative)));

    public static int CountDone(FoodLog? log, MealPlanDay day) =>
        day.PlannedMeals.Where((meal, i) => IsDone(log, meal, AlternativeAt(day, i))).Count();

    public static PlannedMeal? AlternativeAt(MealPlanDay day, int index) =>
        index < day.Alternatives.Count ? day.Alternatives[index] : null;

    private static bool Matches(MealEntry entry, PlannedMeal meal) =>
        entry.MealType == meal.MealType
        && entry.FoodItems.Select(f => f.Name).OrderBy(n => n).SequenceEqual(meal.FoodItems.Select(f => f.Name).OrderBy(n => n));
}
