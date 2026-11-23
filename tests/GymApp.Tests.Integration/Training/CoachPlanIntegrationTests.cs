using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Tests.Integration.Abstractions;
using GymApp.Tests.Integration.Pt;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Training;

/// <summary>
/// pt.training_plan, part D: the coach sees each client's plan, who has gone quiet for the gym's
/// "inactive after" days, and can send one reminder a day — on a real SQL Server.
/// </summary>
public sealed class CoachPlanIntegrationTests : BaseIntegrationTest
{
    private const string Overview = "api/v1/training/coach/plan-overview";
    private const string Settings = "api/v1/training/plan-settings";
    private static readonly TimeSpan OutboxTimeout = TimeSpan.FromSeconds(60);

    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _quietId = Guid.NewGuid();
    private readonly Guid _activeId = Guid.NewGuid();
    private readonly Guid _noPlanId = Guid.NewGuid();
    private Guid _gymId;

    public CoachPlanIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Clients_who_went_quiet_come_first_and_the_gym_decides_after_how_many_days()
    {
        var coach = await StartAsync();
        await AssignAsync(coach, _quietId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)));
        var activePlan = await AssignAsync(coach, _activeId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)));
        await WorkOutAsync(_activeId, activePlan);

        var rows = await RowsAsync(coach);
        rows.Select(r => r.GetProperty("traineeId").GetGuid()).Should().Equal(_quietId, _activeId, _noPlanId);
        rows[0].GetProperty("isInactive").GetBoolean().Should().BeTrue();
        rows[0].GetProperty("daysSince").GetInt32().Should().BeInRange(5, 7);
        rows[1].GetProperty("thisWeek").GetInt32().Should().Be(1);
        rows[1].GetProperty("doneCount").GetInt32().Should().Be(1);
        rows[2].GetProperty("planId").ValueKind.Should().Be(JsonValueKind.Null, "a client without a plan is listed so the coach can give one");

        var owner = Client(CreateGymOwnerClient(Guid.NewGuid(), _gymId));
        (await coach.PutAsJsonAsync(Settings, new { inactiveDays = 10 })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await owner.PutAsJsonAsync(Settings, new { inactiveDays = 1 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await owner.PutAsJsonAsync(Settings, new { inactiveDays = 10 })).StatusCode.Should().Be(HttpStatusCode.OK);

        (await PtTestGym.DataAsync(coach, Settings)).GetProperty("inactiveDays").GetInt32().Should().Be(10);
        (await RowsAsync(coach)).Should().OnlyContain(r => !r.GetProperty("isInactive").GetBoolean());
    }

    [Fact]
    public async Task A_coach_reminds_a_client_once_a_day_and_the_trainee_gets_the_push()
    {
        var coach = await StartAsync();
        await AssignAsync(coach, _quietId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)));
        var url = $"api/v1/training/trainees/{_quietId}/plan-nudge";

        (await coach.PostAsJsonAsync(url, new { message = (string?)null })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await coach.PostAsJsonAsync(url, new { message = "يلا يا بطل" })).StatusCode.Should().Be(HttpStatusCode.Conflict);

        var trainee = Client(CreateTraineeClient(_quietId, _gymId));
        await EventuallyAsync(() => HasNotificationAsync(trainee, "training_plan_nudge"), "the trainee is reminded");

        var stranger = Client(CreateTrainerClient(Guid.NewGuid(), _gymId));
        (await stranger.PostAsJsonAsync(url, new { message = (string?)null })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task With_training_plans_off_the_overview_reminders_and_settings_are_closed()
    {
        var coach = await StartAsync();
        await SetFlagAsync(FeatureKeys.PtTrainingPlan, enabled: false);

        foreach (var response in new[]
                 {
                     await coach.GetAsync(Overview),
                     await coach.GetAsync(Settings),
                     await coach.PostAsJsonAsync($"api/v1/training/trainees/{_quietId}/plan-nudge", new { message = (string?)null }),
                 })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await response.Content.ReadAsStringAsync()).Should().Contain("FEATURE_DISABLED");
        }
    }

    // ------------------------------------------------------------------ helpers

    private async Task<HttpClient> StartAsync()
    {
        _gymId = await PtTestGym.RegisterAsync(Factory.Services, _quietId);
        var deskId = Guid.NewGuid();
        var desk = Client(CreateStaffClient(deskId, _gymId));
        foreach (var member in new[] { _quietId, _activeId, _noPlanId })
        {
            var assigned = await desk.PostAsJsonAsync("api/v1/billing/packages/quick-assign", new
            {
                gymId = _gymId, trainerId = _coachId, memberId = member, sessionCount = 4, assignedBy = deskId, isTrial = true,
            });
            assigned.StatusCode.Should().Be(HttpStatusCode.Created, await assigned.Content.ReadAsStringAsync());
        }
        return Client(CreateTrainerClient(_coachId, _gymId));
    }

    private HttpClient Client(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());
        return client;
    }

    private static object Plan() => new
    {
        name = "Full body",
        goal = "GeneralFitness",
        document = new
        {
            schemaVersion = 1,
            weeks = new[]
            {
                new { week = 1, days = new[] { new { title = "Day A", exercises = new object[] { new { name = "Squat", sets = 3, reps = "10" } } } } },
            },
        },
    };

    private static async Task<JsonElement> AssignAsync(HttpClient coach, Guid traineeId, DateOnly startDate)
    {
        var response = await coach.PostAsJsonAsync("api/v1/training/plans", new { traineeId, startDate, content = Plan() });
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        return JsonDocument.Parse(text).RootElement.GetProperty("data").Clone();
    }

    private async Task WorkOutAsync(Guid traineeId, JsonElement plan)
    {
        var trainee = Client(CreateTraineeClient(traineeId, _gymId));
        var dayKey = plan.GetProperty("document").GetProperty("weeks")[0].GetProperty("days")[0].GetProperty("key").GetString();
        var response = await trainee.PutAsJsonAsync($"api/v1/training/me/plan-workouts/{Guid.NewGuid()}", new
        {
            planId = plan.GetProperty("summary").GetProperty("id").GetGuid(),
            dayKey,
            completedAt = DateTime.UtcNow,
            sets = Array.Empty<object>(),
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private static async Task<List<JsonElement>> RowsAsync(HttpClient coach) =>
        (await PtTestGym.DataAsync(coach, Overview)).GetProperty("clients").EnumerateArray().ToList();

    private async Task SetFlagAsync(string key, bool enabled)
    {
        using var scope = Factory.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IFeatureFlagAdminService>();
        (await admin.SetOverrideAsync(_gymId, new FlagOverrideChange(key, enabled, "test"), new AdminActor(Guid.NewGuid(), "test")))
            .IsSuccess.Should().BeTrue();
    }

    private static async Task<bool> HasNotificationAsync(HttpClient client, string type)
    {
        var data = await PtTestGym.DataAsync(client, "api/v1/communication/notifications?page=1&pageSize=100");
        var items = data.ValueKind == JsonValueKind.Array ? data : data.GetProperty("items");
        return items.EnumerateArray().Any(n => n.GetProperty("data").TryGetProperty("type", out var t) && t.GetString() == type);
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
}
