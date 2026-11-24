using GymApp.Modules.Nutrition.Domain.Diet;
using GymApp.Modules.Nutrition.Domain.Enums;
using GymApp.Modules.Nutrition.Domain.ValueObjects;

namespace GymApp.Modules.Nutrition.Application.Diets;

/// <summary>One meal of a ready diet: what it is called and its portions.</summary>
internal sealed record DietMeal(MealType Type, string TitleAr, string TitleEn, IReadOnlyList<DietPortion> Portions)
{
    public decimal Calories => Portions.Sum(p => p.Food.For(p.Amount).Calories);
}

/// <summary>Name and one-line description, Arabic first.</summary>
internal sealed record DietText(string NameAr, string NameEn, string DescriptionAr, string DescriptionEn);

/// <summary>
/// A ready Egyptian diet (أنظمة أكل جاهزة). Two day variants alternate through the week; each
/// meal slot has one alternative. Portions are written for about 2000 kcal and scaled to the
/// member's target with <see cref="ScaleTo"/>; an alternative is scaled to the calories of the
/// day-A meal it replaces. <see cref="Goal"/> null = fits any goal.
/// </summary>
internal sealed record ReadyDiet(string Key, DietText Text, DietGoal? Goal, IReadOnlyList<DietMeal> DayA, IReadOnlyList<DietMeal> DayB, IReadOnlyList<DietMeal> Alternatives)
{
    private const decimal MinFactor = 0.4m;
    private const decimal MaxFactor = 2.5m;
    private const decimal FactorStep = 0.01m;

    public int MealsPerDay => DayA.Count;

    /// <summary>The week's days scaled to <paramref name="calories"/>: Saturday is day A, then B, A, … (Egyptian week).</summary>
    public IReadOnlyList<ScaledDietDay> ScaleTo(int calories)
    {
        var a = ScaleDay(DayA, calories);
        var b = ScaleDay(DayB, calories);
        var alternatives = Alternatives.Select((alt, i) => ScaleDay(new[] { alt }, a[i].TotalCalories)[0]).ToList();
        return WeekOrder.Select((day, i) => new ScaledDietDay(day, i % 2 == 0 ? a : b, alternatives)).ToList();
    }

    private static readonly DayOfWeek[] WeekOrder =
    {
        DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday,
        DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
    };

    /// <summary>
    /// Portions are rounded (10 g, 50 ml, whole eggs, quarter loaves), so the factor that lands
    /// closest to the target after rounding is found by trying factors 0.40 … 2.50 (the total is
    /// not monotonic in the factor, so a bisection can miss).
    /// </summary>
    private static IReadOnlyList<PlannedMeal> ScaleDay(IReadOnlyList<DietMeal> meals, decimal calories) =>
        Enumerable.Range(0, (int)((MaxFactor - MinFactor) / FactorStep) + 1)
            .Select(i => meals.Select(m => ScaleMeal(m, MinFactor + i * FactorStep)).ToList())
            .MinBy(scaled => Math.Abs(scaled.Sum(m => m.TotalCalories) - calories))!;

    private static PlannedMeal ScaleMeal(DietMeal meal, decimal factor)
    {
        var items = meal.Portions.Select(p => ToItem(p, factor)).ToList();
        return new PlannedMeal
        {
            MealType = meal.Type,
            Name = meal.TitleAr,
            Description = meal.TitleEn,
            FoodItems = items,
            TotalCalories = items.Sum(i => i.Calories),
            TotalProtein = items.Sum(i => i.Protein),
            TotalCarbs = items.Sum(i => i.Carbs),
            TotalFat = items.Sum(i => i.Fat)
        };
    }

    private static FoodItem ToItem(DietPortion portion, decimal factor)
    {
        var food = portion.Food;
        var step = food.Serving.Step;
        var amount = Math.Max(step, Math.Round(portion.Amount * factor / step, MidpointRounding.AwayFromZero) * step);
        var macros = food.For(amount);
        return new FoodItem
        {
            Name = $"{food.NameAr} ({food.NameEn})",
            Quantity = amount,
            Unit = food.Serving.Unit,
            Calories = Math.Round(macros.Calories),
            Protein = Math.Round(macros.Protein, 1),
            Carbs = Math.Round(macros.Carbs, 1),
            Fat = Math.Round(macros.Fat, 1)
        };
    }
}

/// <summary>One day of a scaled diet: its meals and the alternative for each meal slot.</summary>
internal sealed record ScaledDietDay(DayOfWeek DayOfWeek, IReadOnlyList<PlannedMeal> Meals, IReadOnlyList<PlannedMeal> Alternatives)
{
    public decimal TotalCalories => Meals.Sum(m => m.TotalCalories);
}
