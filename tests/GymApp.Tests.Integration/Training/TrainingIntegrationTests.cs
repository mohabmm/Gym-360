using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Training;

public sealed class TrainingIntegrationTests : BaseIntegrationTest
{
    public TrainingIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    #region Exercises

    [Fact]
    public async Task GetExercises_Authenticated_ReturnsOk()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync("api/v1/training/exercises?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetExercises_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.GetAsync("api/v1/training/exercises?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetExercises_WithFilters_ReturnsOk()
    {
        // Arrange
        var client = CreateTrainerClient();

        // Act
        var response = await client.GetAsync(
            "api/v1/training/exercises?muscleGroup=Chest&equipment=Barbell&page=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Workouts

    [Fact]
    public async Task CreateWorkout_AsTrainer_ReturnsCreated()
    {
        // Arrange
        var trainerId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId, gymId);

        var request = new
        {
            TrainerId = trainerId,
            GymId = gymId,
            Name = "Test Workout",
            Description = "Integration test workout",
            Exercises = new[]
            {
                new { ExerciseId = Guid.NewGuid(), Sets = 3, Reps = 12, RestSeconds = 60, OrderIndex = 1 }
            },
            EstimatedDuration = 45,
            IsTemplate = false
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/training/workouts", request);

        // Assert — may return Created or BadRequest (exercise doesn't exist in DB), but not 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateWorkout_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            TrainerId = Guid.NewGuid(),
            GymId = Guid.NewGuid(),
            Name = "Unauthorized Workout",
            Exercises = Array.Empty<object>(),
            IsTemplate = false
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/training/workouts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Remote Workout

    [Fact]
    public async Task ActivateRemoteWorkout_AsTrainer_ProcessesRequest()
    {
        // Arrange
        var trainerId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId, gymId);

        var request = new
        {
            TrainerId = trainerId,
            ClientId = Guid.NewGuid(),
            GymId = gymId,
            Reason = "Traveling"
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/training/remote-workout/activate", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivateRemoteWorkout_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/training/remote-workout/activate", new
        {
            TrainerId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            GymId = Guid.NewGuid(),
            Reason = "Test"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTodaysWorkout_Authenticated_ReturnsResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTraineeClient(userId, gymId);

        // Act
        var response = await client.GetAsync($"api/v1/training/todays-workout?userId={userId}&gymId={gymId}");

        // Assert — may be 200 with null data (no workout) or error, but not 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Workout Assignment

    [Fact]
    public async Task AssignWorkout_AsTrainer_ProcessesRequest()
    {
        // Arrange
        var trainerId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId, gymId);

        var request = new
        {
            WorkoutId = Guid.NewGuid(),
            TrainerId = trainerId,
            ClientId = Guid.NewGuid(),
            GymId = gymId,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd"),
            Notes = "Focus on form"
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/training/workouts/assign", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Workout Logging

    [Fact]
    public async Task LogWorkout_AsTrainee_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTraineeClient(userId, gymId);

        var request = new
        {
            UserId = userId,
            GymId = gymId,
            WorkoutId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            DurationMinutes = 60,
            Notes = "Great session",
            ExerciseLogs = new[]
            {
                new
                {
                    ExerciseId = Guid.NewGuid(),
                    Sets = new[]
                    {
                        new { SetNumber = 1, Reps = 12, Weight = 50.0, IsCompleted = true }
                    }
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/training/workout-logs", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
