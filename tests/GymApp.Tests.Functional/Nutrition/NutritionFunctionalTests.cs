using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Nutrition;

public sealed class NutritionFunctionalTests : BaseFunctionalTest
{
    public NutritionFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    private async Task AssertAnonymousPostRejectedAsync(string url, object body) =>
        AssertStatusCode(await AnonymousClient.PostAsJsonAsync(url, body), HttpStatusCode.Unauthorized);

    #region POST /api/v1/nutrition/food-log

    [Fact]
    public async Task LogFood_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTraineeClient(userId, gymId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/nutrition/food-log", new
        {
            UserId = userId,
            GymId = gymId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            MealType = "breakfast",
            FoodItems = new[]
            {
                new { Name = "Ful Medames", Quantity = 1.0, Unit = "plate", Calories = 350, ProteinG = 15, CarbsG = 40, FatG = 12 }
            }
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public Task LogFood_NoToken_Returns401() =>
        AssertAnonymousPostRejectedAsync("api/v1/nutrition/food-log", new
        {
            UserId = Guid.NewGuid(),
            GymId = Guid.NewGuid(),
            Date = "2026-01-01",
            MealType = "breakfast",
            FoodItems = Array.Empty<object>()
        });

    #endregion

    #region POST /api/v1/nutrition/water-log

    [Fact]
    public async Task LogWater_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTraineeClient(userId, gymId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/nutrition/water-log", new
        {
            UserId = userId,
            GymId = gymId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            AmountMl = 500
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public Task LogWater_NoToken_Returns401() =>
        AssertAnonymousPostRejectedAsync(
            "api/v1/nutrition/water-log",
            new { UserId = Guid.NewGuid(), GymId = Guid.NewGuid(), Date = "2026-01-01", AmountMl = 250 });

    #endregion

    #region GET /api/v1/nutrition/food-log

    [Fact]
    public async Task GetFoodLog_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/nutrition/food-log?userId={userId}&gymId={Guid.NewGuid()}&date={DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFoodLog_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/nutrition/food-log?userId={Guid.NewGuid()}&gymId={Guid.NewGuid()}&date=2026-01-01");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/v1/nutrition/food-log/summary

    [Fact]
    public async Task GetFoodLogSummary_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/nutrition/food-log/summary?userId={userId}&gymId={Guid.NewGuid()}&date={DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/v1/nutrition/water-log

    [Fact]
    public async Task GetWaterLog_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/nutrition/water-log?userId={userId}&gymId={Guid.NewGuid()}&date={DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWaterLog_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/nutrition/water-log?userId={Guid.NewGuid()}&gymId={Guid.NewGuid()}&date=2026-01-01");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Meal Plans

    [Fact]
    public async Task CreateMealPlan_AsTrainer_ProcessesRequest()
    {
        // Arrange
        var trainerId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId, gymId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/nutrition/meal-plans", new
        {
            TrainerId = trainerId,
            GymId = gymId,
            Name = "Egyptian Diet Plan",
            Description = "Traditional Egyptian healthy meals",
            TargetCalories = 2000,
            TargetProteinG = 100,
            TargetCarbsG = 250,
            TargetFatG = 65
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public Task CreateMealPlan_NoToken_Returns401() =>
        AssertAnonymousPostRejectedAsync("api/v1/nutrition/meal-plans", new
        {
            TrainerId = Guid.NewGuid(),
            GymId = Guid.NewGuid(),
            Name = "Test"
        });

    [Fact]
    public async Task GetMealPlans_Authenticated_ProcessesRequest()
    {
        // Arrange
        var client = CreateTrainerClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/nutrition/meal-plans?gymId={Guid.NewGuid()}&trainerId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Food Database Search

    [Fact]
    public async Task SearchFood_Authenticated_ProcessesRequest()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync(
            "api/v1/nutrition/food-database?search=rice&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchFood_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            "api/v1/nutrition/food-database?search=rice&page=1&pageSize=10");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Assign Meal Plan

    [Fact]
    public async Task AssignMealPlan_AsTrainer_ProcessesRequest()
    {
        // Arrange
        var trainerId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/nutrition/meal-plans/assign", new
        {
            MealPlanId = Guid.NewGuid(),
            TrainerId = trainerId,
            ClientId = Guid.NewGuid(),
            GymId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
