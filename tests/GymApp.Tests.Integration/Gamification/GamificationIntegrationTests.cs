using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Gamification;

public sealed class GamificationIntegrationTests : BaseIntegrationTest
{
    public GamificationIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private static string TrainerUrl(string path) =>
        $"api/v1/gamification/trainers/{path}?gymId={Guid.NewGuid()}";

    private static string TrainerUrl(Guid trainerId, string path) => TrainerUrl($"{trainerId}/{path}");

    #region Trainer Ratings

    [Fact]
    public async Task SubmitTrainerRating_AsTrainee_ProcessesRequest()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTraineeClient(clientId, gymId);

        var request = new
        {
            GymId = gymId,
            TrainerId = Guid.NewGuid(),
            ClientId = clientId,
            SessionId = Guid.NewGuid(),
            Stars = 5,
            Comment = "Excellent trainer! Very motivating."
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/gamification/trainer-ratings", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SubmitTrainerRating_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/gamification/trainer-ratings", new
        {
            GymId = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Stars = 4
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SubmitTrainerRating_InvalidStars_ReturnsBadRequest()
    {
        // Arrange
        var client = CreateTraineeClient();

        var request = new
        {
            GymId = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Stars = 0, // Invalid: must be 1-5
            Comment = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/gamification/trainer-ratings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Trainer Ratings View

    [Fact]
    public async Task GetTrainerRatings_Authenticated_ReturnsResponse() =>
        AssertAuthenticated(await CreateGymOwnerClient().GetAsync(TrainerUrl(Guid.NewGuid(), "ratings")));

    [Fact]
    public async Task GetTrainerRatings_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.GetAsync(
            $"api/v1/gamification/trainers/{Guid.NewGuid()}/ratings?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Trainer Public Profile

    [Fact]
    public async Task GetTrainerPublicProfile_Authenticated_ReturnsResponse() =>
        AssertAuthenticated(await CreateTraineeClient().GetAsync(TrainerUrl(Guid.NewGuid(), "public-profile")));

    [Fact]
    public async Task GetTrainerPublicProfile_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.GetAsync(
            $"api/v1/gamification/trainers/{Guid.NewGuid()}/public-profile?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Leaderboards

    [Fact]
    public async Task GetMemberLeaderboard_Authenticated_ReturnsResponse() =>
        AssertAuthenticated(await CreateTraineeClient().GetAsync(
            $"api/v1/gamification/leaderboards/weight_loss?gymId={Guid.NewGuid()}"));

    [Fact]
    public async Task GetMemberLeaderboard_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.GetAsync(
            $"api/v1/gamification/leaderboards/weight_loss?gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("weight_loss")]
    [InlineData("attendance")]
    [InlineData("consistency")]
    public async Task GetMemberLeaderboard_ValidTypes_ReturnsResponse(string type)
    {
        // Arrange
        var client = CreateTraineeClient();
        var gymId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync(
            $"api/v1/gamification/leaderboards/{type}?gymId={gymId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyLeaderboardPosition_Authenticated_ReturnsResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/gamification/leaderboards/my-position?userId={userId}&gymId={Guid.NewGuid()}&type=weight_loss");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Leaderboard Opt-Out

    [Fact]
    public async Task ToggleLeaderboardOptOut_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        var request = new
        {
            UserId = userId,
            GymId = Guid.NewGuid(),
            IsOptedOut = true
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "api/v1/gamification/leaderboards/opt-out", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Coach of the Month

    [Fact]
    public async Task GetCoachOfTheMonth_Authenticated_ReturnsResponse() =>
        AssertAuthenticated(await CreateTraineeClient().GetAsync(TrainerUrl("coach-of-the-month")));

    [Fact]
    public async Task GetCoachLeaderboard_Authenticated_ReturnsResponse() =>
        AssertAuthenticated(await CreateTraineeClient().GetAsync(TrainerUrl("coach-leaderboard")));

    #endregion

    #region Trainer Monthly Scores

    [Fact]
    public async Task GetTrainerMonthlyScores_Authenticated_ReturnsResponse()
    {
        var trainerId = Guid.NewGuid();

        AssertAuthenticated(await CreateTrainerClient(trainerId).GetAsync(TrainerUrl(trainerId, "monthly-scores")));
    }

    #endregion

    #region Testimonial Moderation

    [Fact]
    public async Task ModerateTestimonial_AsOwner_ProcessesRequest()
    {
        // Arrange
        var client = CreateGymOwnerClient();
        var ratingId = Guid.NewGuid();

        var request = new
        {
            RatingId = ratingId,
            IsApproved = true,
            ModeratedBy = Guid.NewGuid()
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/gamification/trainers/ratings/{ratingId}/moderate", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
