using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Nutrition;

public sealed class NutritionIntegrationTests : BaseIntegrationTest
{
    public NutritionIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    #region Food Logging

    [Fact]
    public async Task LogFood_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTraineeClient(userId, gymId);

        var request = new
        {
            UserId = userId,
            GymId = gymId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            MealType = "breakfast",
            FoodItems = new[]
            {
                new { Name = "Eggs", Quantity = 2.0, Unit = "piece", Calories = 140, ProteinG = 12, CarbsG = 1, FatG = 10 }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/nutrition/food-log", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogFood_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/nutrition/food-log", new
        {
            UserId = Guid.NewGuid(),
            GymId = Guid.NewGuid(),
            Date = "2026-01-01",
            MealType = "breakfast",
            FoodItems = Array.Empty<object>()
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFoodLog_Authenticated_ReturnsResponse()
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
    public async Task GetFoodLogSummary_Authenticated_ReturnsResponse()
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

    #region Water Logging

    [Fact]
    public async Task LogWater_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTraineeClient(userId, gymId);

        var request = new
        {
            UserId = userId,
            GymId = gymId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            AmountMl = 500
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/nutrition/water-log", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogWater_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/nutrition/water-log", new
        {
            UserId = Guid.NewGuid(),
            GymId = Guid.NewGuid(),
            Date = "2026-01-01",
            AmountMl = 250
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWaterLog_Authenticated_ReturnsResponse()
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

    #endregion

    #region Meal Plans

    [Fact]
    public async Task CreateMealPlan_AsTrainer_ProcessesRequest()
    {
        // Arrange
        var trainerId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId, gymId);

        var request = new
        {
            TrainerId = trainerId,
            GymId = gymId,
            Name = "Weight Loss Plan",
            Description = "1500 cal daily plan",
            TargetCalories = 1500,
            TargetProteinG = 120,
            TargetCarbsG = 150,
            TargetFatG = 50,
            Meals = new[]
            {
                new
                {
                    Name = "Breakfast",
                    MealType = "breakfast",
                    Items = new[]
                    {
                        new { Name = "Oats", Quantity = 1.0, Unit = "cup", Calories = 300, ProteinG = 10, CarbsG = 50, FatG = 5 }
                    }
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/nutrition/meal-plans", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateMealPlan_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/nutrition/meal-plans", new
        {
            TrainerId = Guid.NewGuid(),
            GymId = Guid.NewGuid(),
            Name = "Unauthorized Plan"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMealPlans_Authenticated_ReturnsResponse()
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
    public async Task SearchFoodDatabase_Authenticated_ReturnsResponse()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync(
            "api/v1/nutrition/food-database?search=rice&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
