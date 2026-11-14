using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Scheduling.Infrastructure.Persistence;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Pt;

/// <summary>
/// pt.exercise_progress over HTTP: during the free trial the coach logs Push / Pull days with
/// numbers, notes and next targets; the trainee sees every number on their board (trial view);
/// after the desk sells a package, paid sessions join the same board.
/// </summary>
public sealed class PtExerciseProgressEndToEndTests : BaseIntegrationTest
{
    private static readonly TimeSpan OutboxTimeout = TimeSpan.FromSeconds(60);

    private Guid _gymId;
    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _traineeId = Guid.NewGuid();
    private readonly Guid _deskId = Guid.NewGuid();

    public PtExerciseProgressEndToEndTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Trial_numbers_reach_the_trainee_and_paid_sessions_join_the_board()
    {
        var (desk, coach, trainee) = await StartAsync();
        (await QuickAssignAsync(desk, new Sale(Sessions: 2, IsTrial: true))).StatusCode.Should().Be(HttpStatusCode.Created);

        // Trial session 1: Push Day with a next target. Trial session 2: Pull Day.
        var push = await QuickLogAsync(coach);
        await LogExercisesAsync(coach, new SessionLog(push, "Push Day", Bench(new Lift("10,8,8", 60m, new { sets = 3, reps = "10", weight = 62.5m }))));
        await AgePreviousQuickLogsAsync();
        var pull = await QuickLogAsync(coach);
        await LogExercisesAsync(coach, new SessionLog(pull, "Pull Day", new { exerciseName = "Barbell row", orderIndex = 0, sets = 3, reps = "10", weight = 50m, notes = "Squeeze at the top" }));

        await EventuallyAsync(async () => (await BoardAsync(trainee)).GetProperty("summary").GetProperty("trialSessionsUsed").GetInt32() == 2,
            "both trial sessions are counted");
        var board = await BoardAsync(trainee);
        var summary = board.GetProperty("summary");
        summary.GetProperty("isTrialView").GetBoolean().Should().BeTrue();
        summary.GetProperty("trialSessionsTotal").GetInt32().Should().Be(2);
        summary.GetProperty("sessionsLogged").GetInt32().Should().Be(2);
        board.GetProperty("days").EnumerateArray().Select(d => d.GetProperty("label").GetString())
            .Should().BeEquivalentTo("Push Day", "Pull Day");
        var bench = Exercise(board, "Bench press");
        bench.GetProperty("nextTarget").GetProperty("weight").GetDecimal().Should().Be(62.5m);
        bench.GetProperty("entries")[0].GetProperty("isTrialSession").GetBoolean().Should().BeTrue();
        bench.GetProperty("entries")[0].GetProperty("notes").GetString().Should().Be("Elbows in");

        // The coach sees the same board; another coach does not.
        (await DataAsync(coach, $"api/v1/scheduling/clients/{_traineeId}/exercise-progress"))
            .GetProperty("exercises").GetArrayLength().Should().Be(2);
        var stranger = CreateTrainerClient(Guid.NewGuid(), _gymId);
        stranger.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());
        (await stranger.GetAsync($"api/v1/scheduling/clients/{_traineeId}/exercise-progress")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);

        // The desk sells a package; the next Push Day hits the target and joins the board.
        await EventuallyAsync(async () => await TrialExhaustedAsync(trainee), "the trial is used up");
        (await QuickAssignAsync(desk, new Sale(Sessions: 8, IsTrial: false, Price: 4000m))).StatusCode.Should().Be(HttpStatusCode.Created);
        await AgePreviousQuickLogsAsync();
        var paid = await QuickLogAsync(coach);
        await LogExercisesAsync(coach, new SessionLog(paid, "Push Day", Bench(new Lift("10,10,10", 62.5m))));

        board = await BoardAsync(trainee);
        board.GetProperty("summary").GetProperty("isTrialView").GetBoolean().Should().BeFalse();
        board.GetProperty("summary").GetProperty("sessionsLogged").GetInt32().Should().Be(3);
        bench = Exercise(board, "Bench press");
        bench.GetProperty("entries").EnumerateArray().Select(e => e.GetProperty("isTrialSession").GetBoolean())
            .Should().Equal(true, false);
        var trend = bench.GetProperty("trend");
        trend.GetProperty("hitPreviousTarget").GetBoolean().Should().BeTrue();
        trend.GetProperty("latestIsPersonalBest").GetBoolean().Should().BeTrue();
        trend.GetProperty("weightChangePercent").GetDecimal().Should().Be(4.2m);
    }

    [Fact]
    public async Task With_the_feature_off_the_board_and_exercise_logging_are_closed()
    {
        var (desk, coach, trainee) = await StartAsync();
        (await QuickAssignAsync(desk, new Sale(Sessions: 1, IsTrial: true))).StatusCode.Should().Be(HttpStatusCode.Created);
        var session = await QuickLogAsync(coach);
        await TurnOffExerciseProgressAsync(); // training.workouts is already off in pt_module

        (await trainee.GetAsync("api/v1/scheduling/me/exercise-progress")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await coach.PostAsJsonAsync($"api/v1/scheduling/sessions/{session}/exercise-logs", new { items = new[] { Bench(new Lift("8", 60m)) } }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------ helpers

    private async Task TurnOffExerciseProgressAsync()
    {
        const string featureKey = FeatureKeys.PtExerciseProgress;
        using var scope = Factory.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IFeatureFlagAdminService>();
        (await admin.SetOverrideAsync(_gymId, new FlagOverrideChange(featureKey, false, "test"), new AdminActor(Guid.NewGuid(), "test")))
            .IsSuccess.Should().BeTrue();
    }

    private sealed record Lift(string Reps, decimal Weight, object? Target = null);

    private sealed record Sale(int Sessions, bool IsTrial, decimal? Price = null);

    private sealed record SessionLog(Guid SessionId, string DayLabel, params object[] Items);

    private static object Bench(Lift lift) =>
        new { exerciseName = "Bench press", orderIndex = 0, sets = 3, reps = lift.Reps, weight = lift.Weight, notes = "Elbows in", target = lift.Target };

    private async Task<(HttpClient Desk, HttpClient Coach, HttpClient Trainee)> StartAsync()
    {
        _gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);
        var desk = CreateStaffClient(_deskId, _gymId);
        var coach = CreateTrainerClient(_coachId, _gymId);
        var trainee = CreateTraineeClient(_traineeId, _gymId);
        foreach (var c in new[] { desk, coach, trainee })
            c.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());
        return (desk, coach, trainee);
    }

    private Task<HttpResponseMessage> QuickAssignAsync(HttpClient desk, Sale sale) =>
        desk.PostAsJsonAsync("api/v1/billing/packages/quick-assign", new
        {
            gymId = _gymId,
            trainerId = _coachId,
            memberId = _traineeId,
            sessionCount = sale.Sessions,
            assignedBy = _deskId,
            totalPrice = sale.Price,
            paymentMethod = sale.Price is > 0 ? "Cash" : null,
            isTrial = sale.IsTrial,
        });

    private async Task<Guid> QuickLogAsync(HttpClient coach)
    {
        var response = await coach.PostAsJsonAsync("api/v1/scheduling/sessions/quick-log", new { memberId = _traineeId });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var data = JsonDocument.Parse(body).RootElement.GetProperty("data");
        data.GetProperty("alreadyLogged").GetBoolean().Should().BeFalse();
        return data.GetProperty("sessionId").GetGuid();
    }

    private static async Task LogExercisesAsync(HttpClient coach, SessionLog log)
    {
        var response = await coach.PostAsJsonAsync(
            $"api/v1/scheduling/sessions/{log.SessionId}/exercise-logs", new { dayLabel = log.DayLabel, items = log.Items });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    /// <summary>Moves earlier quick-logs out of the 5-minute double-tap window (simulates time passing).</summary>
    private async Task AgePreviousQuickLogsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [scheduling].[sessions] SET CreatedAt = DATEADD(minute, -10, CreatedAt) WHERE TrainerId = {_coachId} AND MemberId = {_traineeId}");
    }

    private static Task<JsonElement> BoardAsync(HttpClient trainee) => DataAsync(trainee, "api/v1/scheduling/me/exercise-progress");

    private static JsonElement Exercise(JsonElement board, string name) =>
        board.GetProperty("exercises").EnumerateArray().Single(e => e.GetProperty("name").GetString() == name);

    private static async Task<bool> TrialExhaustedAsync(HttpClient trainee)
    {
        var data = await DataAsync(trainee, "api/v1/billing/me/packages?status=All");
        var items = data.ValueKind == JsonValueKind.Array ? data : data.GetProperty("items");
        return items.EnumerateArray().Any(p => p.GetProperty("isTrial").GetBoolean() && p.GetProperty("status").GetString() == "Exhausted");
    }

    private static Task<JsonElement> DataAsync(HttpClient client, string url) => PtTestGym.DataAsync(client, url);

    private static async Task EventuallyAsync(Func<Task<bool>> condition, string because)
    {
        var deadline = DateTime.UtcNow + OutboxTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        (await condition()).Should().BeTrue(because);
    }
}
