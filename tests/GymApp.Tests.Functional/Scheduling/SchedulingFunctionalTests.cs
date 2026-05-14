using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Scheduling;

public sealed class SchedulingFunctionalTests : BaseFunctionalTest
{
    public SchedulingFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    #region POST /api/v1/scheduling/sessions

    [Fact]
    public async Task BookSession_ValidData_ProcessesRequest()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var client = CreateTraineeClient(memberId, gymId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/scheduling/sessions", new
        {
            GymId = gymId,
            TrainerId = Guid.NewGuid(),
            MemberId = memberId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd"),
            Time = "14:00:00",
            DurationMinutes = 60,
            SessionType = "personal",
            Focus = "Upper body"
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BookSession_InvalidData_Returns400()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.PostAsJsonAsync("api/v1/scheduling/sessions", new
        {
            GymId = Guid.Empty,
            TrainerId = Guid.Empty,
            MemberId = Guid.Empty,
            Date = "",
            Time = "",
            DurationMinutes = 0,
            SessionType = ""
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BookSession_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/scheduling/sessions", new
        {
            GymId = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            Date = "2026-06-01",
            Time = "10:00:00",
            DurationMinutes = 60,
            SessionType = "personal"
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/v1/scheduling/sessions/{id}/cancel

    [Fact]
    public async Task CancelSession_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{Guid.NewGuid()}/cancel",
            new { CancelledBy = userId, Reason = "Can't make it" });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CancelSession_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{Guid.NewGuid()}/cancel",
            new { CancelledBy = Guid.NewGuid(), Reason = "Test" });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/v1/scheduling/sessions/{id}/trainer-log

    [Fact]
    public async Task TrainerLog_AsTrainer_ProcessesRequest()
    {
        // Arrange
        var trainerId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{Guid.NewGuid()}/trainer-log",
            new
            {
                TrainerId = trainerId,
                ExercisesPerformed = "Squats, Bench Press",
                ClientEffortRating = 8,
                Feedback = "Good session",
                AttendanceConfirmed = true
            });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TrainerLog_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{Guid.NewGuid()}/trainer-log",
            new { TrainerId = Guid.NewGuid(), ExercisesPerformed = "Test", ClientEffortRating = 5, AttendanceConfirmed = true });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/v1/scheduling/sessions/{id}/member-log

    [Fact]
    public async Task MemberLog_AsTrainee_ProcessesRequest()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var client = CreateTraineeClient(memberId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{Guid.NewGuid()}/member-log",
            new
            {
                MemberId = memberId,
                Rating = 4,
                PerceivedExertion = 7,
                PersonalNotes = "Great session",
                Satisfaction = 8
            });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MemberLog_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{Guid.NewGuid()}/member-log",
            new { MemberId = Guid.NewGuid(), Rating = 5, PerceivedExertion = 5, Satisfaction = 5 });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/v1/scheduling/sessions

    [Fact]
    public async Task GetSessions_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/scheduling/sessions?userId={userId}&gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSessions_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/scheduling/sessions?userId={Guid.NewGuid()}&gymId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/v1/scheduling/sessions/upcoming

    [Fact]
    public async Task GetUpcoming_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/scheduling/sessions/upcoming?userId={userId}&gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
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

        // Act
        var response = await client.PostAsJsonAsync("api/v1/scheduling/availability", new
        {
            TrainerId = trainerId,
            GymId = gymId,
            DayOfWeek = 1,
            StartTime = "09:00:00",
            EndTime = "17:00:00"
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAvailableSlots_Authenticated_ProcessesRequest()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/scheduling/available-slots?trainerId={Guid.NewGuid()}&gymId={Guid.NewGuid()}&date={DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)):yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region No-Show

    [Fact]
    public async Task MarkNoShow_AsTrainer_ProcessesRequest()
    {
        // Arrange
        var client = CreateTrainerClient();

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{Guid.NewGuid()}/no-show",
            new { MarkedBy = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
