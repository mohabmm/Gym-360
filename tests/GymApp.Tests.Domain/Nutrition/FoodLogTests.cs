using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Enums;
using GymApp.Modules.Nutrition.Domain.Events;
using GymApp.Modules.Nutrition.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Nutrition;

public sealed class FoodLogTests
{
    private static FoodLog CreateDefaultFoodLog()
    {
        return FoodLog.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2025, 6, 15));
    }

    private static List<FoodItem> CreateSampleFoodItems()
    {
        return new List<FoodItem>
        {
            new("Foul Medames", 200, "g", 250, 15, 30, 5),
            new("Baladi Bread", 100, "g", 280, 8, 55, 2)
        };
    }

    [Fact]
    public void Create_ShouldSetPropertiesCorrectly()
    {
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var date = new DateOnly(2025, 6, 15);

        var log = FoodLog.Create(userId, gymId, date);

        log.Id.Should().NotBeEmpty();
        log.UserId.Should().Be(userId);
        log.GymId.Should().Be(gymId);
        log.Date.Should().Be(date);
        log.DailyTotals.Should().Be(NutritionTotals.Zero);
        log.Meals.Should().BeEmpty();
    }

    [Fact]
    public void AddMeal_ShouldAddMealAndRecalculateTotals()
    {
        var log = CreateDefaultFoodLog();
        var items = CreateSampleFoodItems();

        var meal = log.AddMeal(MealType.Breakfast, items);

        log.Meals.Should().ContainSingle();
        meal.MealType.Should().Be(MealType.Breakfast);
        meal.FoodItems.Should().HaveCount(2);

        // 250 + 280 = 530 calories
        log.DailyTotals.TotalCalories.Should().Be(530);
        log.DailyTotals.TotalProtein.Should().Be(23);
        log.DailyTotals.TotalCarbs.Should().Be(85);
        log.DailyTotals.TotalFat.Should().Be(7);
    }

    [Fact]
    public void AddMeal_ShouldRaiseFoodLoggedDomainEvent()
    {
        var log = CreateDefaultFoodLog();

        log.AddMeal(MealType.Lunch, CreateSampleFoodItems());

        log.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<FoodLoggedDomainEvent>();
    }

    [Fact]
    public void AddMeal_MultipleMeals_ShouldAccumulateTotals()
    {
        var log = CreateDefaultFoodLog();

        log.AddMeal(MealType.Breakfast, new List<FoodItem>
        {
            new("Eggs", 150, "g", 220, 18, 1, 15)
        });
        log.AddMeal(MealType.Lunch, new List<FoodItem>
        {
            new("Chicken", 200, "g", 330, 40, 0, 14)
        });

        log.Meals.Should().HaveCount(2);
        log.DailyTotals.TotalCalories.Should().Be(550);
        log.DailyTotals.TotalProtein.Should().Be(58);
    }

    [Fact]
    public void RemoveMeal_ExistingMeal_ShouldRemoveAndRecalculate()
    {
        var log = CreateDefaultFoodLog();
        var meal1 = log.AddMeal(MealType.Breakfast, new List<FoodItem>
        {
            new("Eggs", 150, "g", 220, 18, 1, 15)
        });
        log.AddMeal(MealType.Lunch, new List<FoodItem>
        {
            new("Chicken", 200, "g", 330, 40, 0, 14)
        });

        var result = log.RemoveMeal(meal1.Id);

        result.IsSuccess.Should().BeTrue();
        log.Meals.Should().ContainSingle();
        log.DailyTotals.TotalCalories.Should().Be(330);
    }

    [Fact]
    public void RemoveMeal_NonExistent_ShouldFail()
    {
        var log = CreateDefaultFoodLog();

        var result = log.RemoveMeal(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemoveMeal_AllMeals_ShouldResetTotalsToZero()
    {
        var log = CreateDefaultFoodLog();
        var meal = log.AddMeal(MealType.Snack, new List<FoodItem>
        {
            new("Apple", 150, "g", 80, 0.4m, 21, 0.2m)
        });

        log.RemoveMeal(meal.Id);

        log.DailyTotals.Should().Be(NutritionTotals.Zero);
    }
}

public sealed class NutritionTotalsTests
{
    [Fact]
    public void Zero_ShouldHaveAllZeroes()
    {
        var zero = NutritionTotals.Zero;

        zero.TotalCalories.Should().Be(0);
        zero.TotalProtein.Should().Be(0);
        zero.TotalCarbs.Should().Be(0);
        zero.TotalFat.Should().Be(0);
    }

    [Fact]
    public void Addition_ShouldSumAllFields()
    {
        var a = new NutritionTotals(100, 10, 20, 5);
        var b = new NutritionTotals(200, 20, 30, 10);

        var sum = a + b;

        sum.TotalCalories.Should().Be(300);
        sum.TotalProtein.Should().Be(30);
        sum.TotalCarbs.Should().Be(50);
        sum.TotalFat.Should().Be(15);
    }
}
