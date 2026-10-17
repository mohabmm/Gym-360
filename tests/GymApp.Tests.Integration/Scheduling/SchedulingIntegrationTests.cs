using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Scheduling;

public sealed class SchedulingIntegrationTests : BaseIntegrationTest
{
    public SchedulingIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    #region Book Session

    [Fact]
    public async Task BookSession_Authenticated_ProcessesRequest()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var trainerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var client = CreateTraineeClient(memberId, gymId);

        var request = new
        {
            GymId = gymId,
            TrainerId = trainerId,
            MemberId = memberId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)).ToString("yyyy-MM-dd"),
            Time = "10:00:00",
            DurationMinutes = 60,
            SessionType = "personal",
            Focus = "Strength training"
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/scheduling/sessions", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BookSession_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            GymId = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            Date = "2026-05-01",
            Time = "10:00:00",
            DurationMinutes = 60,
            SessionType = "personal"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/scheduling/sessions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BookSession_InvalidTime_ReturnsBadRequest()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var client = CreateTraineeClient(gymId: gymId);

        var request = new
        {
            GymId = gymId,
            TrainerId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            Date = "invalid-date",
            Time = "invalid-time",
            DurationMinutes = -10,
            SessionType = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/scheduling/sessions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Cancel Session

    [Fact]
    public async Task CancelSession_Authenticated_ProcessesRequest()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        var request = new
        {
            CancelledBy = userId,
            Reason = "Schedule conflict"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{sessionId}/cancel", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CancelSession_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{Guid.NewGuid()}/cancel",
            new { CancelledBy = Guid.NewGuid(), Reason = "Test" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Session Logs

    [Fact]
    public async Task LogTrainerSession_AsTrainer_ProcessesRequest()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var trainerId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId);

        var request = new
        {
            TrainerId = trainerId,
            ExercisesPerformed = "Squat, Bench Press, Deadlift",
            ActualVsPlanned = "Completed as planned",
            ClientEffortRating = 8,
            Feedback = "Great effort today",
            TechniqueNotes = "Form improved on squats",
            AreasToImprove = "Lower back flexibility",
            NextSessionFocus = "Upper body pull",
            AttendanceConfirmed = true
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{sessionId}/trainer-log", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogMemberSession_AsMember_ProcessesRequest()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var client = CreateTraineeClient(memberId);

        var request = new
        {
            MemberId = memberId,
            Rating = 5,
            PerceivedExertion = 7,
            PersonalNotes = "Felt good, energy was high",
            Discomfort = (string?)null,
            Satisfaction = 9
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{sessionId}/member-log", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogTrainerSession_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{Guid.NewGuid()}/trainer-log",
            new { TrainerId = Guid.NewGuid(), ExercisesPerformed = "Test", ClientEffortRating = 5, AttendanceConfirmed = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Availability

    [Fact]
    public async Task SetAvailability_AsTrainer_ProcessesRequest()
    {
        // Arrange
        var trainerId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId, gymId);

        var request = new
        {
            TrainerId = trainerId,
            GymId = gymId,
            DayOfWeek = 1,
            StartTime = "09:00:00",
            EndTime = "17:00:00"
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/scheduling/availability", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAvailability_Authenticated_ReturnsResponse() =>
        AssertAuthenticated(await CreateTraineeClient().GetAsync(
            $"api/v1/scheduling/availability?trainerId={Guid.NewGuid()}&gymId={Guid.NewGuid()}"));

    #endregion

    #region Get Sessions

    [Fact]
    public async Task GetSessions_Authenticated_ReturnsResponse() =>
        AssertAuthenticated(await GetAsTraineeAsync(
            userId => $"api/v1/scheduling/sessions?userId={userId}&gymId={Guid.NewGuid()}"));

    [Fact]
    public async Task GetUpcomingSessions_Authenticated_ReturnsResponse() =>
        AssertAuthenticated(await GetAsTraineeAsync(
            userId => $"api/v1/scheduling/sessions/upcoming?userId={userId}&gymId={Guid.NewGuid()}"));

    #endregion

    #region No-Show

    [Fact]
    public async Task MarkNoShow_Authenticated_ProcessesRequest() =>
        AssertAuthenticated(await CreateTrainerClient().PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{Guid.NewGuid()}/no-show", new { MarkedBy = Guid.NewGuid() }));

    #endregion
}
