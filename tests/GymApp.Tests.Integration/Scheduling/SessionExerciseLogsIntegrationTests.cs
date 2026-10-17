using System.Net;
using System.Net.Http.Json;
using GymApp.Common.Application.Tenancy;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.ValueObjects;
using GymApp.Modules.Scheduling.Infrastructure.Persistence;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Scheduling;

public sealed class SessionExerciseLogsIntegrationTests : BaseIntegrationTest
{
    public SessionExerciseLogsIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private static string LogsUrl(Guid sessionId) => $"api/v1/scheduling/sessions/{sessionId}/exercise-logs";

    private static object SquatLogs(int sets) =>
        new { Items = new[] { new { ExerciseName = "Squat", OrderIndex = 0, Sets = sets, Reps = "10" } } };

    [Fact]
    public async Task PostExerciseLogs_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await HttpClient.PostAsJsonAsync(LogsUrl(Guid.NewGuid()), SquatLogs(sets: 3));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetExerciseLogs_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await HttpClient.GetAsync(
            $"api/v1/scheduling/sessions/{Guid.NewGuid()}/exercise-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostExerciseLogs_EmptyItems_ReturnsBadRequest()
    {
        var trainerId = Guid.NewGuid();
        var client = CreateTrainerClient(trainerId);

        var sessionId = await SeedCompletedSessionAsync(trainerId, Guid.NewGuid());

        var response = await client.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{sessionId}/exercise-logs",
            new { Items = Array.Empty<object>() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostExerciseLogs_InvalidPayload_RejectsZeroSets()
    {
        var trainerId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(trainerId, Guid.NewGuid());

        var response = await CreateTrainerClient(trainerId).PostAsJsonAsync(LogsUrl(sessionId), SquatLogs(sets: 0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostExerciseLogs_NonCompletedSession_ReturnsBadRequest()
    {
        var trainerId = Guid.NewGuid();
        var sessionId = await SeedSessionAsync(trainerId, Guid.NewGuid(), SessionStatus.Pending);

        var response = await CreateTrainerClient(trainerId).PostAsJsonAsync(LogsUrl(sessionId), SquatLogs(sets: 3));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostExerciseLogs_AsTrainer_PersistsAndIsReadableByMember()
    {
        var trainerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(trainerId, memberId);

        var trainerClient = CreateTrainerClient(trainerId);
        var postResponse = await trainerClient.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{sessionId}/exercise-logs",
            new
            {
                Items = new object[]
                {
                    new { ExerciseName = "Back Squat", ExerciseNameAr = "سكوات", OrderIndex = 0, Sets = 4, Reps = "5", Weight = 100.0m, Notes = "Belt on top set" },
                    new { ExerciseName = "Bench Press",                                  OrderIndex = 1, Sets = 3, Reps = "8", Weight = 70.0m,  Notes = "Pause reps" },
                    new { ExerciseName = "Plank",                                         OrderIndex = 2, Sets = 3, Reps = "60s",                Notes = (string?)null }
                }
            });

        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var memberClient = CreateTraineeClient(memberId);
        var getResponse = await memberClient.GetAsync(
            $"api/v1/scheduling/sessions/{sessionId}/exercise-logs");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await getResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Back Squat");
        body.Should().Contain("Bench Press");
        body.Should().Contain("Plank");
    }

    [Fact]
    public async Task GetExerciseLogs_AsForeignTrainee_ReturnsForbidden()
    {
        var trainerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(trainerId, memberId);

        await CreateTrainerClient(trainerId).PostAsJsonAsync(LogsUrl(sessionId), SquatLogs(sets: 3));

        var foreignClient = CreateTraineeClient(Guid.NewGuid());
        var response = await foreignClient.GetAsync(
            $"api/v1/scheduling/sessions/{sessionId}/exercise-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateAndDeleteExerciseLog_AsTrainer_Succeeds()
    {
        var trainerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(trainerId, memberId);

        var trainerClient = CreateTrainerClient(trainerId);
        var postResponse = await trainerClient.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{sessionId}/exercise-logs",
            new
            {
                Items = new[]
                {
                    new { ExerciseName = "Squat", OrderIndex = 0, Sets = 3, Reps = "10", Weight = 80.0m }
                }
            });

        var created = await postResponse.Content.ReadFromJsonAsync<ApiEnvelope<List<LogDto>>>();
        var logId = created!.Data![0].Id;

        var put = await trainerClient.PutAsJsonAsync(
            $"api/v1/scheduling/sessions/{sessionId}/exercise-logs/{logId}",
            new { ExerciseName = "Front Squat", OrderIndex = 0, Sets = 4, Reps = "8", Weight = 90.0m, Notes = "tempo" });
        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var del = await trainerClient.DeleteAsync(
            $"api/v1/scheduling/sessions/{sessionId}/exercise-logs/{logId}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<Guid> SeedCompletedSessionAsync(Guid trainerId, Guid memberId)
        => await SeedSessionAsync(trainerId, memberId, SessionStatus.Completed);

    private async Task<Guid> SeedSessionAsync(Guid trainerId, Guid memberId, SessionStatus targetStatus)
    {
        using var scope = Factory.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        accessor.Set(Guid.NewGuid(), string.Empty, isOwnerScope: false);

        var ctx = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();

        var session = Session.Create(
            new SessionParticipants(Guid.NewGuid(), trainerId, memberId),
            new SessionSchedule(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), new TimeOnly(10, 0), 60),
            SessionType.PersonalTraining);

        if (targetStatus != SessionStatus.Pending)
        {
            session.Confirm(DateTime.UtcNow);
            if (targetStatus is SessionStatus.InProgress or SessionStatus.Completed)
            {
                session.StartSession(DateTime.UtcNow, new EarlyStartPolicy(15, true));
                if (targetStatus == SessionStatus.Completed)
                    session.Complete();
            }
        }

        ctx.Sessions.Add(session);
        await ctx.SaveChangesAsync();
        return session.Id;
    }

    private sealed record ApiEnvelope<T>(T? Data);
    private sealed record LogDto(Guid Id, string ExerciseName, int Sets, string Reps);
}
