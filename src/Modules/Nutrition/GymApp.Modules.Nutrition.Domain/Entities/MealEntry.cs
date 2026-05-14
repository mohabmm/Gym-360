using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Domain.Enums;
using GymApp.Modules.Nutrition.Domain.ValueObjects;

namespace GymApp.Modules.Nutrition.Domain.Entities;

public sealed class MealEntry : Entity
{
    private readonly List<FoodItem> _foodItems = new();

    private MealEntry() { }

    public Guid FoodLogId { get; private set; }
    public MealType MealType { get; private set; }
    public decimal TotalCalories { get; private set; }
    public decimal TotalProtein { get; private set; }
    public decimal TotalCarbs { get; private set; }
    public decimal TotalFat { get; private set; }

    public IReadOnlyList<FoodItem> FoodItems => _foodItems.AsReadOnly();

    public static MealEntry Create(
        Guid foodLogId,
        MealType mealType,
        IEnumerable<FoodItem> foodItems)
    {
        var entry = new MealEntry
        {
            Id = Guid.NewGuid(),
            FoodLogId = foodLogId,
            MealType = mealType
        };

        foreach (var item in foodItems)
        {
            entry._foodItems.Add(item);
        }

        entry.RecalculateTotals();

        return entry;
    }

    public void AddFoodItem(FoodItem item)
    {
        _foodItems.Add(item);
        RecalculateTotals();
    }

    public void RemoveFoodItem(FoodItem item)
    {
        _foodItems.Remove(item);
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        TotalCalories = _foodItems.Sum(f => f.Calories);
        TotalProtein = _foodItems.Sum(f => f.Protein);
        TotalCarbs = _foodItems.Sum(f => f.Carbs);
        TotalFat = _foodItems.Sum(f => f.Fat);
    }
}
