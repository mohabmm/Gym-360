using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Domain.Enums;
using GymApp.Modules.Nutrition.Domain.Errors;
using GymApp.Modules.Nutrition.Domain.Events;
using GymApp.Modules.Nutrition.Domain.ValueObjects;

namespace GymApp.Modules.Nutrition.Domain.Entities;

public sealed class FoodLog : Entity
{
    private readonly List<MealEntry> _meals = new();

    private FoodLog() { }

    public Guid UserId { get; private set; }
    public Guid GymId { get; private set; }
    public DateOnly Date { get; private set; }
    public NutritionTotals DailyTotals { get; private set; } = NutritionTotals.Zero;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<MealEntry> Meals => _meals.AsReadOnly();

    public static FoodLog Create(Guid userId, Guid gymId, DateOnly date)
    {
        var log = new FoodLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GymId = gymId,
            Date = date,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return log;
    }

    public MealEntry AddMeal(MealType mealType, IEnumerable<FoodItem> foodItems)
    {
        var meal = MealEntry.Create(Id, mealType, foodItems);
        _meals.Add(meal);
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new FoodLoggedDomainEvent(Id, UserId));

        return meal;
    }

    public Result RemoveMeal(Guid mealEntryId)
    {
        var meal = _meals.FirstOrDefault(m => m.Id == mealEntryId);
        if (meal is null)
            return Result.Failure(NutritionErrors.FoodLogNotFound(mealEntryId));

        _meals.Remove(meal);
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public void RecalculateTotals()
    {
        DailyTotals = _meals.Aggregate(
            NutritionTotals.Zero,
            (totals, meal) => totals + new NutritionTotals(
                meal.TotalCalories,
                meal.TotalProtein,
                meal.TotalCarbs,
                meal.TotalFat));
    }
}
