using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Progress;

public sealed class ProgressFunctionalTests : BaseFunctionalTest
{
    public ProgressFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    #region POST /api/v1/progress/body-metrics

    [Fact]
    public async Task LogBodyMetric_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTraineeClient(userId, gymId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/progress/body-metrics", new
        {
            UserId = userId,
            GymId = gymId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            WeightKg = 82.5,
            BodyFatPercentage = 20.0,
            MuscleMassKg = 34.0,
            WaistCm = 88.0,
            Notes = "Morning measurement after fasting"
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogBodyMetric_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/progress/body-metrics", new
        {
            UserId = Guid.NewGuid(),
            GymId = Guid.NewGuid(),
            Date = "2026-01-01",
            WeightKg = 80.0
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/v1/progress/body-metrics

    [Fact]
    public async Task GetBodyMetrics_Authenticated_ProcessesRequest()
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
    public async Task GetBodyMetrics_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/progress/body-metrics?userId={Guid.NewGuid()}&gymId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/v1/progress/body-metrics/trends

    [Fact]
    public async Task GetTrends_Authenticated_ProcessesRequest()
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

    [Fact]
    public async Task GetTrends_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/progress/body-metrics/trends?userId={Guid.NewGuid()}&gymId={Guid.NewGuid()}&metric=weight&months=3");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Progress Photos

    [Fact]
    public async Task GetPhotos_Authenticated_ProcessesRequest()
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
    public async Task GetPhotos_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/progress/photos?userId={Guid.NewGuid()}&gymId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePhoto_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.DeleteAsync(
            $"api/v1/progress/photos/{Guid.NewGuid()}?userId={userId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Dashboard

    [Fact]
    public async Task GetDashboard_Authenticated_ProcessesRequest()
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

    [Fact]
    public async Task GetDashboard_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/progress/dashboard?userId={Guid.NewGuid()}&gymId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Client Progress (Trainer View)

    [Fact]
    public async Task GetClientProgress_AsTrainer_ProcessesRequest()
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

    [Fact]
    public async Task GetClientProgress_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/progress/clients/{Guid.NewGuid()}?gymId={Guid.NewGuid()}&trainerId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Log Body Metric for Client (Trainer)

    [Fact]
    public async Task LogBodyMetricForClient_AsTrainer_ProcessesRequest()
    {
        // Arrange
        var trainerId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId, gymId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/progress/clients/{Guid.NewGuid()}/body-metrics",
            new
            {
                TrainerId = trainerId,
                GymId = gymId,
                ClientId = Guid.NewGuid(),
                Date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                WeightKg = 75.0,
                Notes = "Measured after training"
            });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
