using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Gamification;

public sealed class GamificationFunctionalTests : BaseFunctionalTest
{
    public GamificationFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    #region POST /api/v1/gamification/trainer-ratings

    [Fact]
    public async Task SubmitRating_AsTrainee_ProcessesRequest()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTraineeClient(clientId, gymId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/gamification/trainer-ratings", new
        {
            GymId = gymId,
            TrainerId = Guid.NewGuid(),
            ClientId = clientId,
            SessionId = Guid.NewGuid(),
            Stars = 5,
            Comment = "Amazing trainer!"
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SubmitRating_InvalidStars_Returns400()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.PostAsJsonAsync("api/v1/gamification/trainer-ratings", new
        {
            GymId = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Stars = 0 // Invalid
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitRating_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/gamification/trainer-ratings", new
        {
            GymId = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Stars = 4
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task SubmitRating_ValidStarValues_ProcessesRequest(int stars)
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.PostAsJsonAsync("api/v1/gamification/trainer-ratings", new
        {
            GymId = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Stars = stars,
            Comment = $"Rating of {stars} stars"
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/v1/gamification/trainers/{id}/ratings

    [Fact]
    public async Task GetTrainerRatings_Authenticated_ProcessesRequest()
    {
        // Arrange
        var client = CreateOwnerClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/gamification/trainers/{Guid.NewGuid()}/ratings?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTrainerRatings_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/gamification/trainers/{Guid.NewGuid()}/ratings?gymId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/v1/gamification/trainers/{id}/public-profile

    [Fact]
    public async Task GetPublicProfile_Authenticated_ProcessesRequest()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/gamification/trainers/{Guid.NewGuid()}/public-profile?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPublicProfile_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/gamification/trainers/{Guid.NewGuid()}/public-profile?gymId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/v1/gamification/leaderboards/{type}

    [Theory]
    [InlineData("weight_loss")]
    [InlineData("attendance")]
    [InlineData("consistency")]
    public async Task GetLeaderboard_ValidType_ProcessesRequest(string type)
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/gamification/leaderboards/{type}?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLeaderboard_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/gamification/leaderboards/weight_loss?gymId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Opt-Out

    [Fact]
    public async Task ToggleOptOut_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/gamification/leaderboards/opt-out", new
        {
            UserId = userId,
            GymId = Guid.NewGuid(),
            IsOptedOut = true
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Coach of Month

    [Fact]
    public async Task GetCoachOfMonth_Authenticated_ProcessesRequest()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/gamification/trainers/coach-of-the-month?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
