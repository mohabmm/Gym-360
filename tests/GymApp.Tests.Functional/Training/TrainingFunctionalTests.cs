using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Training;

public sealed class TrainingFunctionalTests : BaseFunctionalTest
{
    public TrainingFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    #region GET /api/v1/training/exercises

    [Fact]
    public async Task GetExercises_Authenticated_Returns200()
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync("api/v1/training/exercises?page=1&pageSize=10");

        // Assert
        AssertStatusCode(response, HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetExercises_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync("api/v1/training/exercises?page=1&pageSize=10");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Chest")]
    [InlineData("Back")]
    [InlineData("Legs")]
    public async Task GetExercises_ByMuscleGroup_Returns200(string muscleGroup)
    {
        // Arrange
        var client = CreateTraineeClient();

        // Act
        var response = await client.GetAsync(
            $"api/v1/training/exercises?muscleGroup={muscleGroup}&page=1&pageSize=10");

        // Assert
        AssertStatusCode(response, HttpStatusCode.OK);
    }

    #endregion

    #region POST /api/v1/training/workouts

    [Fact]
    public async Task CreateWorkout_AsTrainer_ProcessesRequest()
    {
        // Arrange
        var trainerId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId, gymId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/training/workouts", new
        {
            TrainerId = trainerId,
            GymId = gymId,
            Name = "Chest Day",
            Description = "Focus on chest and triceps",
            Exercises = new[]
            {
                new { ExerciseId = Guid.NewGuid(), Sets = 4, Reps = 10, RestSeconds = 90, OrderIndex = 1 }
            },
            EstimatedDuration = 60,
            IsTemplate = false
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateWorkout_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/training/workouts", new
        {
            TrainerId = Guid.NewGuid(),
            GymId = Guid.NewGuid(),
            Name = "Unauthorized",
            Exercises = Array.Empty<object>(),
            IsTemplate = false
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/v1/training/remote-workout/activate

    [Fact]
    public async Task ActivateRemoteWorkout_AsTrainer_ProcessesRequest()
    {
        // Arrange
        var trainerId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId, gymId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/training/remote-workout/activate", new
        {
            TrainerId = trainerId,
            ClientId = Guid.NewGuid(),
            GymId = gymId,
            Reason = "Client is traveling"
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivateRemoteWorkout_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync(
            "api/v1/training/remote-workout/activate",
            new { TrainerId = Guid.NewGuid(), ClientId = Guid.NewGuid(), GymId = Guid.NewGuid(), Reason = "Test" });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/v1/training/todays-workout

    [Fact]
    public async Task GetTodaysWorkout_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var client = CreateTraineeClient(userId, gymId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/training/todays-workout?userId={userId}&gymId={gymId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTodaysWorkout_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/training/todays-workout?userId={Guid.NewGuid()}&gymId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Workout Logs

    [Fact]
    public async Task GetWorkoutLogs_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/training/workout-logs?userId={userId}&gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWorkoutLogs_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/training/workout-logs?userId={Guid.NewGuid()}&gymId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion
}
