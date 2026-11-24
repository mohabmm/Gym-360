using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Infrastructure.Data;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Tests.Integration.Abstractions;
using GymApp.Tests.Integration.Pt;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Training;

/// <summary>
/// pt.starter_plan on a real SQL Server: the coach is told about a new client and gives a 3-day
/// starter plan; each staff unlock allows one one-week program from a template; limited plans end on their own.
/// </summary>
public sealed class StarterPlanIntegrationTests : BaseIntegrationTest
{
    private const string Plans = "api/v1/training/plans";
    private const string Unlocks = "api/v1/training/plan-unlocks";
    private static readonly TimeSpan OutboxTimeout = TimeSpan.FromSeconds(60);

    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _traineeId = Guid.NewGuid();
    private readonly Guid _deskId = Guid.NewGuid();
    private Guid _gymId;

    public StarterPlanIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private string Access => $"api/v1/training/trainees/{_traineeId}/plan-access";

    [Fact]
    public async Task New_client_push_then_a_3_day_plan_then_one_week_per_staff_unlock()
    {
        var (coach, desk, _) = await StartAsync(flagOn: true);
        await EventuallyAsync(async () => await CountAsync(coach, "new_client_assigned") == 1, "the coach is told about the new client");

        await AccessShouldBeAsync(coach, ExpectedAccess.Starter);
        await ShouldBeRefusedAsync(await AssignAsync(coach, new PlanRequest(new Shape(1, 4))), TrainingPlanErrors.StarterPlanOnly);
        var starter = await AssignAsync(coach, new PlanRequest(new Shape(1, 3)));
        starter.StatusCode.Should().Be(HttpStatusCode.OK, await starter.Content.ReadAsStringAsync());
        var summary = (await DataOf(starter)).GetProperty("summary");
        summary.GetProperty("limit").GetString().Should().Be("Starter");
        summary.GetProperty("endsOn").GetString().Should().Be(Today.AddDays(2).ToString("yyyy-MM-dd"));

        // The member stops coming: the desk unlocks one week from the templates.
        (await desk.PutAsJsonAsync(Unlocks, new { trainerId = _coachId, traineeId = _traineeId, unlocked = true }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        await EventuallyAsync(async () => await CountAsync(coach, "plan_programs_unlocked") == 1, "the coach is told the week is unlocked");
        await AccessShouldBeAsync(coach, ExpectedAccess.OneWeek);
        await ShouldBeRefusedAsync(await AssignAsync(coach, new PlanRequest(new Shape(4, 4))), TrainingPlanErrors.WeekProgramOnly);

        var templateId = (await PtTestGym.DataAsync(coach, "api/v1/training/plan-templates"))[0].GetProperty("id").GetGuid();
        var week = await AssignAsync(coach, new PlanRequest(new Shape(1, 5)) { TemplateId = templateId });
        week.StatusCode.Should().Be(HttpStatusCode.OK, await week.Content.ReadAsStringAsync());
        summary = (await DataOf(week)).GetProperty("summary");
        summary.GetProperty("limit").GetString().Should().Be("OneWeek");
        summary.GetProperty("endsOn").GetString().Should().Be(Today.AddDays(6).ToString("yyyy-MM-dd"));

        await AccessShouldBeAsync(coach, ExpectedAccess.Starter); // the week used the unlock up
        var row = (await PtTestGym.DataAsync(desk, $"{Unlocks}?traineeId={_traineeId}")).GetProperty("coaches").EnumerateArray().Should().ContainSingle().Subject;
        row.GetProperty("unlocked").GetBoolean().Should().BeFalse();
        row.GetProperty("limitedPlan").GetProperty("limit").GetString().Should().Be("OneWeek");
        row.GetProperty("limitedPlan").GetProperty("hasEnded").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task The_desk_unlocks_30_days_and_the_coach_gives_up_to_four_weeks()
    {
        var (coach, desk, _) = await StartAsync(flagOn: true);
        (await desk.PutAsJsonAsync(Unlocks, new { trainerId = _coachId, traineeId = _traineeId, unlocked = true, length = "ThirtyDays" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        await AccessShouldBeAsync(coach, ExpectedAccess.ThirtyDays);
        await ShouldBeRefusedAsync(await AssignAsync(coach, new PlanRequest(new Shape(5, 3))), TrainingPlanErrors.ThirtyDaysOnly);
        var month = await AssignAsync(coach, new PlanRequest(new Shape(4, 3)));
        month.StatusCode.Should().Be(HttpStatusCode.OK, await month.Content.ReadAsStringAsync());
        var summary = (await DataOf(month)).GetProperty("summary");
        summary.GetProperty("limit").GetString().Should().Be("ThirtyDays");
        summary.GetProperty("endsOn").GetString().Should().Be(Today.AddDays(29).ToString("yyyy-MM-dd"));

        var row = (await PtTestGym.DataAsync(desk, $"{Unlocks}?traineeId={_traineeId}")).GetProperty("coaches")[0];
        row.GetProperty("unlocked").GetBoolean().Should().BeFalse("the 30-day program used the unlock up");
        row.GetProperty("length").GetString().Should().Be("ThirtyDays");
        await AccessShouldBeAsync(coach, ExpectedAccess.Starter);
    }

    [Fact]
    public async Task The_desk_can_take_back_an_unused_unlock()
    {
        var (coach, desk, _) = await StartAsync(flagOn: true);
        (await desk.PutAsJsonAsync(Unlocks, new { trainerId = _coachId, traineeId = _traineeId, unlocked = true })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await desk.PutAsJsonAsync(Unlocks, new { trainerId = _coachId, traineeId = _traineeId, unlocked = false })).StatusCode.Should().Be(HttpStatusCode.OK);

        await AccessShouldBeAsync(coach, ExpectedAccess.Starter);
    }

    [Fact]
    public async Task Only_staff_unlock_and_only_for_a_coach_with_a_package()
    {
        var (coach, desk, trainee) = await StartAsync(flagOn: true);
        var body = new { trainerId = _coachId, traineeId = _traineeId, unlocked = true };

        (await coach.PutAsJsonAsync(Unlocks, body)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await trainee.PutAsJsonAsync(Unlocks, body)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await desk.PutAsJsonAsync(Unlocks, new { trainerId = Guid.NewGuid(), traineeId = _traineeId, unlocked = true }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound, "that coach has no package with the member");
    }

    [Fact]
    public async Task A_starter_plan_is_gone_from_the_trainee_app_after_its_third_day()
    {
        var (coach, _, trainee) = await StartAsync(flagOn: true);
        (await AssignAsync(coach, new PlanRequest(new Shape(1, 3)) { Start = Today.AddDays(-3) })).StatusCode.Should().Be(HttpStatusCode.OK);

        (await PtTestGym.DataAsync(trainee, "api/v1/training/me/plan")).ValueKind.Should().Be(JsonValueKind.Null);
        (await PtTestGym.DataAsync(coach, Access)).GetProperty("limitedPlan").GetProperty("hasEnded").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task With_the_flag_off_coaches_work_as_before_and_the_unlock_endpoints_are_closed()
    {
        var (coach, desk, _) = await StartAsync(flagOn: false);

        await AccessShouldBeAsync(coach, ExpectedAccess.None);
        (await AssignAsync(coach, new PlanRequest(new Shape(4, 4)))).StatusCode.Should().Be(HttpStatusCode.OK);
        var closed = await desk.GetAsync($"{Unlocks}?traineeId={_traineeId}");
        closed.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await closed.Content.ReadAsStringAsync()).Should().Contain("FEATURE_DISABLED");
        await Task.Delay(TimeSpan.FromSeconds(5));
        (await CountAsync(coach, "new_client_assigned")).Should().Be(0, "no new-client push while the flag is off");
    }

    // ------------------------------------------------------------------ helpers

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task<(HttpClient Coach, HttpClient Desk, HttpClient Trainee)> StartAsync(bool flagOn)
    {
        _gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);
        using (var scope = Factory.Services.CreateScope())
        {
            var gymsDb = scope.ServiceProvider.GetRequiredService<GymsDbContext>();
            gymsDb.StaffRoles.Add(StaffRole.Create(_gymId, _deskId, StaffRoleType.Receptionist));
            await gymsDb.SaveChangesAsync();
        }
        if (flagOn)
            await EnableStarterPlanAsync();

        var desk = Client(CreateStaffClient(_deskId, _gymId));
        var assigned = await desk.PostAsJsonAsync("api/v1/billing/packages/quick-assign", new
        {
            gymId = _gymId, trainerId = _coachId, memberId = _traineeId, sessionCount = 2, assignedBy = _deskId, isTrial = true,
        });
        assigned.StatusCode.Should().Be(HttpStatusCode.Created, await assigned.Content.ReadAsStringAsync());
        return (Client(CreateTrainerClient(_coachId, _gymId)), desk, Client(CreateTraineeClient(_traineeId, _gymId)));
    }

    private HttpClient Client(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());
        return client;
    }

    /// <summary>A plan of <paramref name="Weeks"/> weeks with <paramref name="Days"/> workout days each.</summary>
    private sealed record Shape(int Weeks, int Days);

    private sealed record PlanRequest(Shape Shape)
    {
        public DateOnly? Start { get; init; }
        public Guid? TemplateId { get; init; }
    }

    /// <summary>What plan-access should say: the limit, the days it lasts and its most weeks (0 = no limit).</summary>
    private sealed record ExpectedAccess(string Limit, int LengthDays, int MaxWeeks)
    {
        public static readonly ExpectedAccess Starter = new("Starter", 3, 1);
        public static readonly ExpectedAccess OneWeek = new("OneWeek", 7, 1);
        public static readonly ExpectedAccess ThirtyDays = new("ThirtyDays", 30, 4);
        public static readonly ExpectedAccess None = new("None", 0, 0);
    }

    private static object Document(Shape shape) => new
    {
        schemaVersion = 1,
        weeks = Enumerable.Range(1, shape.Weeks).Select(w => new
        {
            week = w,
            days = Enumerable.Range(1, shape.Days).Select(d => new
            {
                title = $"Day {d}",
                exercises = new object[] { new { name = "Squat", sets = 3, reps = "10" } },
            }).ToArray(),
        }).ToArray(),
    };

    private Task<HttpResponseMessage> AssignAsync(HttpClient coach, PlanRequest request) =>
        coach.PostAsJsonAsync(Plans, new
        {
            traineeId = _traineeId,
            startDate = request.Start ?? Today,
            templateId = request.TemplateId,
            content = new { name = "Plan", goal = "GeneralFitness", document = Document(request.Shape) },
        });

    private async Task AccessShouldBeAsync(HttpClient coach, ExpectedAccess expected)
    {
        var access = await PtTestGym.DataAsync(coach, Access);
        access.GetProperty("limit").GetString().Should().Be(expected.Limit);
        access.GetProperty("lengthDays").GetInt32().Should().Be(expected.LengthDays);
        access.GetProperty("maxWeeks").GetInt32().Should().Be(expected.MaxWeeks);
    }

    private static async Task ShouldBeRefusedAsync(HttpResponseMessage response, Error expected)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().Contain(expected.Code);
    }

    private static async Task<JsonElement> DataOf(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("data").Clone();

    private async Task EnableStarterPlanAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IFeatureFlagAdminService>();
        (await admin.SetOverrideAsync(_gymId, new FlagOverrideChange(FeatureKeys.PtStarterPlan, true, "test"), new AdminActor(Guid.NewGuid(), "test")))
            .IsSuccess.Should().BeTrue();
    }

    private static async Task<int> CountAsync(HttpClient client, string type)
    {
        var data = await PtTestGym.DataAsync(client, "api/v1/communication/notifications?page=1&pageSize=100");
        var items = data.ValueKind == JsonValueKind.Array ? data : data.GetProperty("items");
        return items.EnumerateArray().Count(n =>
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
