using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Nutrition;

public sealed class NutritionIntegrationTests : BaseIntegrationTest
{
    public NutritionIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private static string Today() => DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

    /// <summary>POSTs as a fresh trainee of a fresh gym; both ids are threaded into the body.</summary>
    private Task<HttpResponseMessage> PostAsGymTraineeAsync(string url, Func<Guid, Guid, object> body)
    {
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        return CreateTraineeClient(userId, gymId).PostAsJsonAsync(url, body(userId, gymId));
    }

    private async Task AssertAnonymousPostRejectedAsync(string url, object body) =>
        (await HttpClient.PostAsJsonAsync(url, body)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    #region Food Logging

    [Fact]
    public async Task LogFood_Authenticated_ProcessesRequest() =>
        AssertAuthenticated(await PostAsGymTraineeAsync("api/v1/nutrition/food-log", (userId, gymId) => new
        {
            UserId = userId,
            GymId = gymId,
            Date = Today(),
            MealType = "breakfast",
            FoodItems = new[]
            {
                new { Name = "Eggs", Quantity = 2.0, Unit = "piece", Calories = 140, ProteinG = 12, CarbsG = 1, FatG = 10 }
            }
        }));

    [Fact]
    public Task LogFood_Unauthenticated_ReturnsUnauthorized() =>
        AssertAnonymousPostRejectedAsync("api/v1/nutrition/food-log", new
        {
            UserId = Guid.NewGuid(),
            GymId = Guid.NewGuid(),
            Date = "2026-01-01",
            MealType = "breakfast",
            FoodItems = Array.Empty<object>()
        });

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
    public async Task LogWater_Authenticated_ProcessesRequest() =>
        AssertAuthenticated(await PostAsGymTraineeAsync(
            "api/v1/nutrition/water-log",
            (userId, gymId) => new { UserId = userId, GymId = gymId, Date = Today(), AmountMl = 500 }));

    [Fact]
    public Task LogWater_Unauthenticated_ReturnsUnauthorized() =>
        AssertAnonymousPostRejectedAsync(
            "api/v1/nutrition/water-log",
            new { UserId = Guid.NewGuid(), GymId = Guid.NewGuid(), Date = "2026-01-01", AmountMl = 250 });

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
    public Task CreateMealPlan_Unauthenticated_ReturnsUnauthorized() =>
        AssertAnonymousPostRejectedAsync(
            "api/v1/nutrition/meal-plans",
            new { TrainerId = Guid.NewGuid(), GymId = Guid.NewGuid(), Name = "Unauthorized Plan" });

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
