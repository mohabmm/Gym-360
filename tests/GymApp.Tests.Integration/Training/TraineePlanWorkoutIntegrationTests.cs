using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Tests.Integration.Abstractions;
using GymApp.Tests.Integration.Pt;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Training;

/// <summary>
/// pt.training_plan, part B: the trainee's "today's workout", offline uploads that are safe to
/// retry or arrive late, and progress — on a real SQL Server.
/// </summary>
public sealed class TraineePlanWorkoutIntegrationTests : BaseIntegrationTest
{
    private const string MyPlan = "api/v1/training/me/plan";
    private const string MyProgress = "api/v1/training/me/plan-progress";

    private static readonly TimeSpan OutboxTimeout = TimeSpan.FromSeconds(60);

    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _traineeId = Guid.NewGuid();
    private Guid _gymId;

    public TraineePlanWorkoutIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Finishing_today_s_workout_moves_on_to_the_next_day_and_a_retried_upload_counts_once()
    {
        var (coach, trainee) = await StartAsync();
        var planId = await AssignAsync(coach, TwoDayPlan("Ahmed"));

        var today = await PtTestGym.DataAsync(trainee, MyPlan);
        var dayA = NextDay(today);
        today.GetProperty("cursor").GetProperty("doneToday").GetBoolean().Should().BeFalse();

        var clientId = Guid.NewGuid();
        var first = await UploadAsync(trainee, clientId, Workout(planId, dayA, squatKg: 40));
        first.GetProperty("outcome").GetString().Should().Be("created");
        var retry = await UploadAsync(trainee, clientId, Workout(planId, dayA, squatKg: 40));
        retry.GetProperty("outcome").GetString().Should().Be("already_synced");
        retry.GetProperty("workout").GetProperty("id").GetGuid().Should().Be(first.GetProperty("workout").GetProperty("id").GetGuid());

        var after = await PtTestGym.DataAsync(trainee, MyPlan);
        NextDay(after).Should().NotBe(dayA);
        after.GetProperty("cursor").GetProperty("doneToday").GetBoolean().Should().BeTrue();
        after.GetProperty("stats").GetProperty("thisWeek").GetInt32().Should().Be(1, "the retry did not count twice");
        after.GetProperty("lastWeights").EnumerateObject().Select(p => p.Value.GetDecimal()).Should().Contain(40m, "next time the squat starts at 40 kg");
    }

    [Fact]
    public async Task The_same_day_finished_twice_on_one_date_is_kept_once()
    {
        var (coach, trainee) = await StartAsync();
        var planId = await AssignAsync(coach, TwoDayPlan("P"));
        var dayA = NextDay(await PtTestGym.DataAsync(trainee, MyPlan));
        await UploadAsync(trainee, Guid.NewGuid(), Workout(planId, dayA, squatKg: 40));

        var otherPhone = await UploadAsync(trainee, Guid.NewGuid(), Workout(planId, dayA, squatKg: 45));

        otherPhone.GetProperty("outcome").GetString().Should().Be("duplicate");
        (await PtTestGym.DataAsync(trainee, MyProgress)).GetProperty("history").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task A_late_offline_upload_is_still_saved_after_the_coach_changed_or_replaced_the_plan()
    {
        var (coach, trainee) = await StartAsync();
        var planId = await AssignAsync(coach, TwoDayPlan("Old"));
        var dayA = NextDay(await PtTestGym.DataAsync(trainee, MyPlan));
        var yesterday = DateTime.UtcNow.AddDays(-1);

        // The coach rebuilds the plan (new days, new keys) while the phone is offline.
        var edit = await coach.PutAsJsonAsync($"api/v1/training/plans/{planId}", new { expectedVersion = 1, content = TwoDayPlan("Rebuilt") });
        edit.StatusCode.Should().Be(HttpStatusCode.OK, await edit.Content.ReadAsStringAsync());
        var changed = await UploadAsync(trainee, Guid.NewGuid(), Workout(planId, dayA, squatKg: 40, completedAt: yesterday));
        changed.GetProperty("outcome").GetString().Should().Be("plan_changed");
        changed.GetProperty("workout").GetProperty("dayTitle").GetString().Should().Be("Day A", "the day as the phone saw it");

        await AssignAsync(coach, TwoDayPlan("Brand new"));
        var replaced = await UploadAsync(trainee, Guid.NewGuid(), Workout(planId, "d-gone", squatKg: 40, completedAt: yesterday.AddHours(-30)));
        replaced.GetProperty("outcome").GetString().Should().Be("plan_replaced");

        var progress = await PtTestGym.DataAsync(trainee, MyProgress);
        progress.GetProperty("history").GetArrayLength().Should().Be(2, "no real workout is thrown away");
        progress.GetProperty("stats").GetProperty("thisMonth").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Only_the_trainee_uploads_to_their_plan_and_only_their_coach_or_the_gym_managers_see_progress()
    {
        var (coach, trainee) = await StartAsync();
        var planId = await AssignAsync(coach, TwoDayPlan("P"));
        var dayA = NextDay(await PtTestGym.DataAsync(trainee, MyPlan));
        await UploadAsync(trainee, Guid.NewGuid(), Workout(planId, dayA, squatKg: 60));

        var stranger = Client(CreateTraineeClient(Guid.NewGuid(), _gymId));
        (await stranger.PutAsJsonAsync($"api/v1/training/me/plan-workouts/{Guid.NewGuid()}", Workout(planId, dayA, 60)))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await PtTestGym.DataAsync(stranger, MyPlan)).ValueKind.Should().Be(JsonValueKind.Null, "no plan yet");

        var url = $"api/v1/training/trainees/{_traineeId}/plan-progress";
        var seen = await PtTestGym.DataAsync(coach, url);
        seen.GetProperty("exercises").EnumerateArray().Should().ContainSingle(e => e.GetProperty("name").GetString() == "Goblet Squat");
        (await Client(CreateGymOwnerClient(Guid.NewGuid(), _gymId)).GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Client(CreateTrainerClient(Guid.NewGuid(), _gymId)).GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task With_training_plans_off_the_trainee_plan_endpoints_are_closed()
    {
        var (coach, trainee) = await StartAsync();
        var planId = await AssignAsync(coach, TwoDayPlan("P"));
        await SetFlagAsync(FeatureKeys.PtTrainingPlan, enabled: false);

        foreach (var response in new[]
                 {
                     await trainee.GetAsync(MyPlan),
                     await trainee.GetAsync(MyProgress),
                     await trainee.PutAsJsonAsync($"api/v1/training/me/plan-workouts/{Guid.NewGuid()}", Workout(planId, "d-a", 40)),
                 })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await response.Content.ReadAsStringAsync()).Should().Contain("FEATURE_DISABLED");
        }
    }

    [Fact]
    public async Task A_pt_session_logged_on_a_plan_day_counts_for_the_trainee_until_the_desk_reverses_it()
    {
        var (coach, trainee) = await StartAsync();
        var planId = await AssignAsync(coach, TwoDayPlan("P"));
        var dayA = NextDay(await PtTestGym.DataAsync(trainee, MyPlan));

        var logged = await coach.PostAsJsonAsync("api/v1/scheduling/sessions/quick-log", new { memberId = _traineeId, planDay = new { planId, dayKey = dayA } });
        logged.StatusCode.Should().Be(HttpStatusCode.OK, await logged.Content.ReadAsStringAsync());
        var sessionId = JsonDocument.Parse(await logged.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("sessionId").GetGuid();

        await EventuallyAsync(async () => NextDay(await PtTestGym.DataAsync(trainee, MyPlan)) != dayA, "the session moved the plan on");
        var history = (await PtTestGym.DataAsync(trainee, MyProgress)).GetProperty("history").EnumerateArray().Should().ContainSingle().Subject;
        history.GetProperty("source").GetString().Should().Be("CoachSession");

        (await trainee.PostAsJsonAsync($"api/v1/scheduling/sessions/{sessionId}/member-dispute", new { reason = "Not me" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent, "dual confirmation still works");
        NextDay(await PtTestGym.DataAsync(trainee, MyPlan)).Should().NotBe(dayA, "a dispute alone changes nothing until the desk decides");

        (await _desk!.PostAsync($"api/v1/scheduling/sessions/{sessionId}/reverse", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await EventuallyAsync(async () => NextDay(await PtTestGym.DataAsync(trainee, MyPlan)) == dayA, "a reversed session no longer counts");
        (await PtTestGym.DataAsync(trainee, MyPlan)).GetProperty("stats").GetProperty("thisWeek").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task The_same_day_done_in_the_app_and_in_the_coach_s_session_counts_once()
    {
        var (coach, trainee) = await StartAsync();
        var planId = await AssignAsync(coach, TwoDayPlan("P"));
        var dayA = NextDay(await PtTestGym.DataAsync(trainee, MyPlan));
        await UploadAsync(trainee, Guid.NewGuid(), Workout(planId, dayA, squatKg: 40));

        (await coach.PostAsJsonAsync("api/v1/scheduling/sessions/quick-log", new { memberId = _traineeId, planDay = new { planId, dayKey = dayA } }))
            .EnsureSuccessStatusCode();
        await EventuallyAsync(async () => await RemainingAsync(trainee) == 3, "the session was processed");

        (await PtTestGym.DataAsync(trainee, MyProgress)).GetProperty("history").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task With_training_plans_off_a_session_with_a_plan_day_is_still_logged_and_nothing_else_happens()
    {
        var (coach, trainee) = await StartAsync();
        var planId = await AssignAsync(coach, TwoDayPlan("P"));
        var dayA = NextDay(await PtTestGym.DataAsync(trainee, MyPlan));
        await SetFlagAsync(FeatureKeys.PtTrainingPlan, enabled: false);

        var logged = await coach.PostAsJsonAsync("api/v1/scheduling/sessions/quick-log", new { memberId = _traineeId, planDay = new { planId, dayKey = dayA } });

        logged.StatusCode.Should().Be(HttpStatusCode.OK, "session logging never depends on plans");
        await EventuallyAsync(async () => await RemainingAsync(trainee) == 3, "the package is still deducted");
        await SetFlagAsync(FeatureKeys.PtTrainingPlan, enabled: true);
        NextDay(await PtTestGym.DataAsync(trainee, MyPlan)).Should().Be(dayA);
    }

    // ------------------------------------------------------------------ helpers

    private HttpClient? _desk;

    private static async Task<int> RemainingAsync(HttpClient trainee)
    {
        var data = await PtTestGym.DataAsync(trainee, "api/v1/billing/me/packages");
        var items = data.ValueKind == JsonValueKind.Array ? data : data.GetProperty("items");
        return items.EnumerateArray()
            .Where(p => p.TryGetProperty("status", out var s) && s.GetString() == "Active")
            .Sum(p => p.GetProperty("remainingSessions").GetInt32());
    }

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

    private static string NextDay(JsonElement myPlan) => myPlan.GetProperty("cursor").GetProperty("nextDayKey").GetString()!;

    private static object Workout(Guid planId, string dayKey, decimal squatKg, DateTime? completedAt = null) => new
    {
        planId,
        dayKey,
        completedAt = completedAt ?? DateTime.UtcNow,
        planVersion = 1,
        dayTitle = "Day A",
        dayTitleAr = "اليوم أ",
        week = 1,
        sequenceIndex = 0,
        durationMinutes = 45,
        sets = new object[]
        {
            new { exerciseKey = "e-squat", exerciseName = "Goblet Squat", setNumber = 1, done = true, reps = 12, weightKg = squatKg },
            new { exerciseKey = "e-squat", exerciseName = "Goblet Squat", setNumber = 2, done = false, reps = 0 },
            new { exerciseKey = "e-plank", exerciseName = "Plank", setNumber = 1, done = true, seconds = 30 },
        },
    };

    private static object TwoDayPlan(string name) => new
    {
        name,
        goal = "GeneralFitness",
        document = new
        {
            schemaVersion = 1,
            weeks = new[]
            {
                new
                {
                    week = 1,
                    days = new[]
                    {
                        new { title = "Day A", titleAr = "اليوم أ", exercises = new object[] { new { name = "Goblet Squat", sets = 2, reps = "12" }, new { name = "Plank", sets = 1, seconds = 30 } } },
                        new { title = "Day B", titleAr = "اليوم ب", exercises = new object[] { new { name = "Push-up", sets = 3, reps = "10" } } },
                    },
                },
            },
        },
    };

    private async Task<(HttpClient Coach, HttpClient Trainee)> StartAsync()
    {
        _gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);
        var deskId = Guid.NewGuid();
        var desk = Client(CreateStaffClient(deskId, _gymId));
        _desk = desk;
        var assigned = await desk.PostAsJsonAsync("api/v1/billing/packages/quick-assign", new
        {
            gymId = _gymId, trainerId = _coachId, memberId = _traineeId, sessionCount = 4, assignedBy = deskId, isTrial = true,
        });
        assigned.StatusCode.Should().Be(HttpStatusCode.Created, await assigned.Content.ReadAsStringAsync());
        return (Client(CreateTrainerClient(_coachId, _gymId)), Client(CreateTraineeClient(_traineeId, _gymId)));
    }

    private HttpClient Client(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());
        return client;
    }

    private async Task<Guid> AssignAsync(HttpClient coach, object content)
    {
        var response = await coach.PostAsJsonAsync("api/v1/training/plans", new { traineeId = _traineeId, content });
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        return JsonDocument.Parse(text).RootElement.GetProperty("data").GetProperty("summary").GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> UploadAsync(HttpClient trainee, Guid clientId, object workout)
    {
        var response = await trainee.PutAsJsonAsync($"api/v1/training/me/plan-workouts/{clientId}", workout);
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        return JsonDocument.Parse(text).RootElement.GetProperty("data").Clone();
    }

    private async Task SetFlagAsync(string key, bool enabled)
    {
        using var scope = Factory.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IFeatureFlagAdminService>();
        (await admin.SetOverrideAsync(_gymId, new FlagOverrideChange(key, enabled, "test"), new AdminActor(Guid.NewGuid(), "test")))
            .IsSuccess.Should().BeTrue();
    }
}
