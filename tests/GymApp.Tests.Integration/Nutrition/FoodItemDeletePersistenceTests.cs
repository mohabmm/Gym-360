using GymApp.Modules.Nutrition.Application.Abstractions;
using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Enums;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Domain.ValueObjects;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Nutrition;

/// <summary>
/// Verifies that deleting a single food item actually persists against real
/// SQL Server. Food items are an owned collection mapped with ToJson, so the
/// edit lives inside a JSON column on the meal_entries row — a case the in-memory
/// domain tests cannot exercise. Each step uses its own DI scope (fresh DbContext)
/// so reads come from the database, not the tracking cache.
/// </summary>
public sealed class FoodItemDeletePersistenceTests : BaseIntegrationTest
{
    public FoodItemDeletePersistenceTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private async Task<(Guid foodLogId, Guid mealId)> SeedMealWithTwoItemsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFoodLogRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<INutritionUnitOfWork>();

        var log = FoodLog.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 5, 31));
        // index 0: normal macros. index 1: a zero-macro item (e.g. water / diet soda).
        var meal = log.AddMeal(MealType.Breakfast, new List<FoodItem>
        {
            new("Foul Medames", 200, "g", 250, 15, 30, 5),
            new("Sparkling Water", 330, "ml", 0, 0, 0, 0),
        });

        await repo.AddAsync(log);
        await uow.SaveChangesAsync();

        return (log.Id, meal.Id);
    }

    [Fact]
    public async Task DeleteFoodItem_ZeroMacroItem_PersistsRemovalEvenThoughTotalsUnchanged()
    {
        // The adversarial case: removing a 0/0/0/0 item leaves daily totals
        // identical, so persistence cannot "ride along" on a changed scalar
        // column — EF must persist the JSON-column mutation itself.
        var (_, mealId) = await SeedMealWithTwoItemsAsync();

        // Act — delete index 1 (the zero-macro item) in a fresh scope.
        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IFoodLogRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<INutritionUnitOfWork>();

            var log = await repo.GetByMealIdAsync(mealId);
            log.Should().NotBeNull();

            var result = log!.RemoveFoodItem(mealId, 1);
            result.IsSuccess.Should().BeTrue();

            await uow.SaveChangesAsync();
        }

        // Assert — re-read from the database in another fresh scope.
        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IFoodLogRepository>();
            var reloaded = await repo.GetByMealIdAsync(mealId);

            reloaded.Should().NotBeNull();
            var meal = reloaded!.Meals.Single(m => m.Id == mealId);
            meal.FoodItems.Should().ContainSingle(because: "the zero-macro item must be gone after delete");
            meal.FoodItems[0].Name.Should().Be("Foul Medames");
        }
    }

    [Fact]
    public async Task DeleteFoodItem_LastItem_PersistsEmptyMealRemoval()
    {
        var (_, mealId) = await SeedMealWithTwoItemsAsync();

        // Delete both items one at a time; meal should be gone after the second.
        for (var i = 0; i < 2; i++)
        {
            using var scope = Factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IFoodLogRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<INutritionUnitOfWork>();

            var log = await repo.GetByMealIdAsync(mealId);
            if (log is null) break; // meal row already gone
            log.RemoveFoodItem(mealId, 0).IsSuccess.Should().BeTrue();
            await uow.SaveChangesAsync();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IFoodLogRepository>();
            var reloaded = await repo.GetByMealIdAsync(mealId);
            reloaded.Should().BeNull(because: "an emptied meal is removed, so no log still owns this mealId");
        }
    }
}
