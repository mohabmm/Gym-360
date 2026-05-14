using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Domain.ValueObjects;

namespace GymApp.Modules.Nutrition.Domain.Entities;

public sealed class MealPlanDay : Entity
{
    private readonly List<PlannedMeal> _plannedMeals = new();
    private readonly List<PlannedMeal> _alternatives = new();

    private MealPlanDay() { }

    public Guid MealPlanId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }

    public IReadOnlyList<PlannedMeal> PlannedMeals => _plannedMeals.AsReadOnly();
    public IReadOnlyList<PlannedMeal> Alternatives => _alternatives.AsReadOnly();

    public static MealPlanDay Create(
        Guid mealPlanId,
        DayOfWeek dayOfWeek,
        IEnumerable<PlannedMeal> plannedMeals,
        IEnumerable<PlannedMeal>? alternatives = null)
    {
        var day = new MealPlanDay
        {
            Id = Guid.NewGuid(),
            MealPlanId = mealPlanId,
            DayOfWeek = dayOfWeek
        };

        day._plannedMeals.AddRange(plannedMeals);

        if (alternatives is not null)
            day._alternatives.AddRange(alternatives);

        return day;
    }

    public void SetPlannedMeals(IEnumerable<PlannedMeal> meals)
    {
        _plannedMeals.Clear();
        _plannedMeals.AddRange(meals);
    }

    public void SetAlternatives(IEnumerable<PlannedMeal> alternatives)
    {
        _alternatives.Clear();
        _alternatives.AddRange(alternatives);
    }
}
