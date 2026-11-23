using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Tests.Integration.Abstractions;
using GymApp.Tests.Integration.Pt;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Training;

/// <summary>
/// pt.training_plan, part A: a coach gives a member on free sessions a plan — from a gym template
/// or built from scratch — edits it, and the template never changes; the flag closes it all.
/// </summary>
public sealed class TrainingPlanIntegrationTests : BaseIntegrationTest
{
    private const string Templates = "api/v1/training/plan-templates";
    private const string Plans = "api/v1/training/plans";
    private static readonly TimeSpan OutboxTimeout = TimeSpan.FromSeconds(60);

    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _traineeId = Guid.NewGuid();
    private readonly Guid _deskId = Guid.NewGuid();
    private Guid _gymId;

    public TrainingPlanIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Coach_assigns_a_gym_sample_template_and_editing_the_plan_leaves_the_template_alone()
    {
        var (_, coach, trainee) = await StartWithFreeSessionsAsync();

        var templates = (await PtTestGym.DataAsync(coach, Templates)).EnumerateArray().ToList();
        templates.Should().HaveCount(3, "every gym starts with three Arabic sample templates");
        templates.Select(t => t.GetProperty("nameAr").GetString()).Should().Contain("فول بادي للمبتدئين – ٣ أيام");
        templates.Should().OnlyContain(t => t.GetProperty("isSample").GetBoolean() && !t.GetProperty("canEdit").GetBoolean());
        var fullBody = templates.First(t => t.GetProperty("daysPerWeek").GetInt32() == 3);
        var templateId = fullBody.GetProperty("id").GetGuid();
        var templateBefore = await TemplateDocumentAsync(coach, templateId);

        var plan = await AssignAsync(coach, new { traineeId = _traineeId, templateId });
        plan.GetProperty("summary").GetProperty("durationWeeks").GetInt32().Should().Be(4);
        plan.GetProperty("summary").GetProperty("workoutCount").GetInt32().Should().Be(12);
        plan.GetProperty("summary").GetProperty("sourceTemplateId").GetGuid().Should().Be(templateId);

        var planId = plan.GetProperty("summary").GetProperty("id").GetGuid();
        var edited = await coach.PutAsJsonAsync($"{Plans}/{planId}", new { expectedVersion = 1, content = Content("Changed for Ahmed", weeks: 2) });
        edited.StatusCode.Should().Be(HttpStatusCode.OK, await edited.Content.ReadAsStringAsync());

        (await TemplateDocumentAsync(coach, templateId)).Should().Be(templateBefore, "a trainee's plan is a copy");
        var mine = await PtTestGym.DataAsync(trainee, $"{Plans}/{planId}");
        mine.GetProperty("summary").GetProperty("name").GetString().Should().Be("Changed for Ahmed");
        mine.GetProperty("summary").GetProperty("version").GetInt32().Should().Be(2);
        await EventuallyAsync(() => HasNotificationAsync(trainee, "training_plan_updated"), "the trainee hears about the edit");
        (await HasNotificationAsync(trainee, "training_plan_assigned")).Should().BeTrue();
    }

    [Fact]
    public async Task Coach_builds_a_plan_from_scratch_with_week_one_copied_to_four_weeks()
    {
        var (_, coach, _) = await StartWithFreeSessionsAsync();

        var plan = await AssignAsync(coach, new { traineeId = _traineeId, content = Content("Ahmed's own plan", weeks: 1, repeatFor: 4) });

        plan.GetProperty("summary").GetProperty("sourceTemplateId").ValueKind.Should().Be(JsonValueKind.Null);
        plan.GetProperty("document").GetProperty("weeks").GetArrayLength().Should().Be(4);
        plan.GetProperty("document").GetProperty("schemaVersion").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task A_new_plan_replaces_the_active_one_and_the_old_one_stays_in_history()
    {
        var (_, coach, _) = await StartWithFreeSessionsAsync();
        var first = await AssignAsync(coach, new { traineeId = _traineeId, content = Content("First", weeks: 1) });
        await AssignAsync(coach, new { traineeId = _traineeId, content = Content("Second", weeks: 1) });

        var plans = await PtTestGym.DataAsync(coach, $"api/v1/training/trainees/{_traineeId}/plans");

        plans.GetProperty("active").GetProperty("summary").GetProperty("name").GetString().Should().Be("Second");
        var old = plans.GetProperty("history").EnumerateArray().Should().ContainSingle().Subject;
        old.GetProperty("id").GetGuid().Should().Be(first.GetProperty("summary").GetProperty("id").GetGuid());
        old.GetProperty("status").GetString().Should().Be("Replaced");
    }

    [Fact]
    public async Task A_stale_edit_is_a_conflict()
    {
        var (_, coach, _) = await StartWithFreeSessionsAsync();
        var plan = await AssignAsync(coach, new { traineeId = _traineeId, content = Content("P", weeks: 1) });
        var url = $"{Plans}/{plan.GetProperty("summary").GetProperty("id").GetGuid()}";
        (await coach.PutAsJsonAsync(url, new { expectedVersion = 1, content = Content("P2", weeks: 1) })).EnsureSuccessStatusCode();

        var stale = await coach.PutAsJsonAsync(url, new { expectedVersion = 1, content = Content("P3", weeks: 1) });

        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_coach_cannot_give_a_plan_to_someone_who_is_not_their_client()
    {
        _gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);
        var coach = Client(CreateTrainerClient(_coachId, _gymId));

        var response = await coach.PostAsJsonAsync(Plans, new { traineeId = _traineeId, content = Content("P", weeks: 1) });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Only_the_owner_or_a_manager_creates_gym_templates_and_every_coach_then_sees_them()
    {
        var (_, coach, _) = await StartWithFreeSessionsAsync();
        var owner = Client(CreateGymOwnerClient(Guid.NewGuid(), _gymId));

        (await coach.PostAsJsonAsync(Templates, new { asGymTemplate = true, content = Content("Coach try", weeks: 1) }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var created = await owner.PostAsJsonAsync(Templates, new { asGymTemplate = true, content = Content("House plan", weeks: 1) });
        created.StatusCode.Should().Be(HttpStatusCode.OK, await created.Content.ReadAsStringAsync());
        (await coach.PostAsJsonAsync(Templates, new { asGymTemplate = false, content = Content("My own", weeks: 1) })).EnsureSuccessStatusCode();

        var otherCoach = Client(CreateTrainerClient(Guid.NewGuid(), _gymId));
        var names = (await PtTestGym.DataAsync(otherCoach, Templates)).EnumerateArray().Select(t => t.GetProperty("name").GetString()).ToList();

        names.Should().Contain("House plan").And.NotContain("My own");
    }

    [Fact]
    public async Task With_training_plans_off_every_plan_endpoint_is_closed_and_session_logging_still_works()
    {
        var (_, coach, trainee) = await StartWithFreeSessionsAsync();
        var plan = await AssignAsync(coach, new { traineeId = _traineeId, content = Content("P", weeks: 1) });
        await SetFlagAsync(FeatureKeys.PtTrainingPlan, enabled: false);

        foreach (var url in new[] { Templates, $"{Plans}/{plan.GetProperty("summary").GetProperty("id").GetGuid()}", $"api/v1/training/trainees/{_traineeId}/plans", "api/v1/training/plan-exercises?search=squat" })
        {
            var response = await coach.GetAsync(url);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden, url);
            (await response.Content.ReadAsStringAsync()).Should().Contain("FEATURE_DISABLED");
        }
        (await coach.PostAsJsonAsync(Plans, new { traineeId = _traineeId, content = Content("P", weeks: 1) }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var logged = await coach.PostAsJsonAsync("api/v1/scheduling/sessions/quick-log", new { memberId = _traineeId });
        logged.StatusCode.Should().Be(HttpStatusCode.OK, "session-by-session logging does not depend on plans");
    }

    [Fact]
    public async Task Plan_builder_finds_library_exercises_by_their_arabic_name()
    {
        var (_, coach, _) = await StartWithFreeSessionsAsync();

        var page = await PtTestGym.DataAsync(coach, "api/v1/training/plan-exercises?search=" + Uri.EscapeDataString("سكوات"));

        page.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        var owner = Client(CreateGymOwnerClient(Guid.NewGuid(), _gymId));
        (await owner.GetAsync("api/v1/training/plan-exercises?search=squat")).StatusCode
            .Should().Be(HttpStatusCode.OK, "the staff web template editor searches the library too");
    }

    // ------------------------------------------------------------------ helpers

    private static object Content(string name, int weeks, int? repeatFor = null) => new
    {
        name,
        nameAr = (string?)null,
        goal = "FatLoss",
        goalText = (string?)null,
        repeatWeekOneForWeeks = repeatFor,
        document = new
        {
            schemaVersion = 1,
            weeks = Enumerable.Range(1, weeks).Select(w => new
            {
                week = w,
                days = new[]
                {
                    new { key = "", title = "Day A", titleAr = "اليوم أ", exercises = new object[]
                    {
                        new { key = "", name = "Goblet Squat", sets = 3, reps = "12", restSeconds = 60 },
                        new { key = "", name = "Plank", sets = 3, seconds = 30, restSeconds = 30 },
                    } },
                },
            }),
        },
    };

    private async Task<(HttpClient Desk, HttpClient Coach, HttpClient Trainee)> StartWithFreeSessionsAsync()
    {
        _gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);
        var desk = Client(CreateStaffClient(_deskId, _gymId));
        var coach = Client(CreateTrainerClient(_coachId, _gymId));
        var trainee = Client(CreateTraineeClient(_traineeId, _gymId));
        var assigned = await desk.PostAsJsonAsync("api/v1/billing/packages/quick-assign", new
        {
            gymId = _gymId, trainerId = _coachId, memberId = _traineeId, sessionCount = 4, assignedBy = _deskId, isTrial = true,
        });
        assigned.StatusCode.Should().Be(HttpStatusCode.Created, await assigned.Content.ReadAsStringAsync());
        return (desk, coach, trainee);
    }

    private HttpClient Client(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());
        return client;
    }

    private static async Task<JsonElement> AssignAsync(HttpClient coach, object body)
    {
        var response = await coach.PostAsJsonAsync(Plans, body);
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        return JsonDocument.Parse(text).RootElement.GetProperty("data").Clone();
    }

    private static async Task<string> TemplateDocumentAsync(HttpClient client, Guid templateId) =>
        (await PtTestGym.DataAsync(client, $"{Templates}/{templateId}")).GetProperty("document").GetRawText();

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
        return items.EnumerateArray().Any(n =>
            n.GetProperty("data").TryGetProperty("type", out var t) && t.GetString() == type);
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
