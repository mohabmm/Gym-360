using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Progress;

public sealed class ProgressIntegrationTests : BaseIntegrationTest
{
    public ProgressIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    #region Body Metrics

    [Fact]
    public async Task LogBodyMetric_Authenticated_ProcessesRequest()
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
            WeightKg = 80.5,
            BodyFatPercentage = 18.0,
            MuscleMassKg = 35.0,
            WaistCm = 85.0,
            ChestCm = 100.0,
            ArmCm = 35.0,
            Notes = "Morning measurement"
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/progress/body-metrics", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogBodyMetric_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/progress/body-metrics", new
        {
            UserId = Guid.NewGuid(),
            GymId = Guid.NewGuid(),
            Date = "2026-01-01",
            WeightKg = 80.0
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBodyMetrics_Authenticated_ReturnsResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/progress/body-metrics?userId={userId}&gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBodyMetricTrends_Authenticated_ReturnsResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/progress/body-metrics/trends?userId={userId}&gymId={Guid.NewGuid()}&metric=weight&months=3");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Progress Photos

    [Fact]
    public async Task GetProgressPhotos_Authenticated_ReturnsResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/progress/photos?userId={userId}&gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProgressPhotos_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.GetAsync(
            $"api/v1/progress/photos?userId={Guid.NewGuid()}&gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Progress Dashboard

    [Fact]
    public async Task GetProgressDashboard_Authenticated_ReturnsResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/progress/dashboard?userId={userId}&gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Client Progress (Trainer View)

    [Fact]
    public async Task GetClientProgress_AsTrainer_ReturnsResponse()
    {
        // Arrange
        var trainerId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/progress/clients/{Guid.NewGuid()}?gymId={Guid.NewGuid()}&trainerId={trainerId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
