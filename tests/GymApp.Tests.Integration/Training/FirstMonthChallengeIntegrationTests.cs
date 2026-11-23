using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Infrastructure.Data;
using GymApp.Tests.Integration.Abstractions;
using GymApp.Tests.Integration.Pt;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Training;

/// <summary>
/// mem.first_month_challenge, part E: plan workouts (app or a PT session on a plan day) inside the
/// window reach the gym's target once; trainee, coach and desk are told once; the desk ticks the
/// reward off — on a real SQL Server.
/// </summary>
public sealed class FirstMonthChallengeIntegrationTests : BaseIntegrationTest
{
    private const string MyChallenge = "api/v1/training/me/challenge";
    private const string Awards = "api/v1/training/challenge/awards";
    private const string Settings = "api/v1/training/plan-settings";
    private const string Summary = "api/v1/training/reports/plan-summary";
    private static readonly TimeSpan OutboxTimeout = TimeSpan.FromSeconds(60);

    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _traineeId = Guid.NewGuid();
    private readonly Guid _deskId = Guid.NewGuid();
    private Guid _gymId;

    public FirstMonthChallengeIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Reaching_the_target_awards_once_tells_trainee_coach_and_desk_once_and_the_desk_ticks_it_off()
    {
        var (coach, trainee, owner) = await StartAsync();
        (await owner.PutAsJsonAsync(Settings, new { challenge = new { target = 3, windowDays = 30, rewardTextAr = "جلسة متابعة مجانية" } }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var plan = await AssignAsync(coach);
        var (planId, days) = (PlanId(plan), DayKeys(plan));

        (await WorkOutAsync(trainee, planId, days[0])).Should().Be("created");
        (await WorkOutAsync(trainee, planId, days[1])).Should().Be("created");
        var progress = await PtTestGym.DataAsync(trainee, MyChallenge);
        progress.GetProperty("status").GetString().Should().Be("active");
        progress.GetProperty("count").GetInt32().Should().Be(2);
        progress.GetProperty("target").GetInt32().Should().Be(3);
        progress.GetProperty("rewardTextAr").GetString().Should().Be("جلسة متابعة مجانية");

        // The third counts from the coach's PT session on a plan day.
        var logged = await coach.PostAsJsonAsync("api/v1/scheduling/sessions/quick-log", new { memberId = _traineeId, planDay = new { planId, dayKey = days[2] } });
        logged.StatusCode.Should().Be(HttpStatusCode.OK, await logged.Content.ReadAsStringAsync());
        await EventuallyAsync(async () => (await PtTestGym.DataAsync(trainee, MyChallenge)).GetProperty("status").GetString() == "reached", "the session reached the target");
        (await WorkOutAsync(trainee, planId, days[0])).Should().Be("duplicate", "the same day again today is one workout");

        var deskClient = Client(CreateStaffClient(_deskId, _gymId));
        foreach (var (client, who) in new[] { (trainee, "trainee"), (coach, "coach"), (deskClient, "desk") })
        {
            await EventuallyAsync(async () => await CountNotificationsAsync(client) == 1, $"the {who} is told");
        }
        await Task.Delay(TimeSpan.FromSeconds(5));
        (await CountNotificationsAsync(trainee)).Should().Be(1, "more workouts after reaching it never announce it again");

        var pending = (await PtTestGym.DataAsync(deskClient, $"{Awards}?delivered=false")).EnumerateArray().Should().ContainSingle().Subject;
        pending.GetProperty("traineeId").GetGuid().Should().Be(_traineeId);
        pending.GetProperty("trainerId").GetGuid().Should().Be(_coachId);
        pending.GetProperty("count").GetInt32().Should().Be(3);
        var awardId = pending.GetProperty("awardId").GetGuid();

        (await deskClient.PutAsJsonAsync($"{Awards}/{awardId}/delivered", new { delivered = true })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await PtTestGym.DataAsync(deskClient, $"{Awards}?delivered=false")).GetArrayLength().Should().Be(0);
        (await PtTestGym.DataAsync(trainee, MyChallenge)).GetProperty("deliveredAt").ValueKind.Should().NotBe(JsonValueKind.Null);

        var summary = await PtTestGym.DataAsync(owner, Summary);
        summary.GetProperty("activePlans").GetInt32().Should().Be(1);
        summary.GetProperty("workoutsThisMonth").GetInt32().Should().BeGreaterThanOrEqualTo(3);
        summary.GetProperty("challengeReached").GetInt32().Should().Be(1);
        summary.GetProperty("challengeAwaitingDelivery").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task A_member_who_joined_before_the_window_does_not_win_and_sees_it_ended()
    {
        var (coach, trainee, owner) = await StartAsync(joinedDaysAgo: 40);
        (await owner.PutAsJsonAsync(Settings, new { challenge = new { target = 1, windowDays = 30 } })).StatusCode.Should().Be(HttpStatusCode.OK);
        var plan = await AssignAsync(coach);

        await WorkOutAsync(trainee, PlanId(plan), DayKeys(plan)[0]);

        var progress = await PtTestGym.DataAsync(trainee, MyChallenge);
        progress.GetProperty("status").GetString().Should().Be("ended", "the window counts from the join date, not from the plan");
        progress.GetProperty("count").GetInt32().Should().Be(0);
        (await PtTestGym.DataAsync(owner, Awards)).GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task With_the_challenge_off_nothing_is_awarded_and_its_endpoints_are_closed()
    {
        var (coach, trainee, owner) = await StartAsync(challengeOn: false);
        var plan = await AssignAsync(coach);
        await WorkOutAsync(trainee, PlanId(plan), DayKeys(plan)[0]);

        foreach (var response in new[]
                 {
                     await trainee.GetAsync(MyChallenge),
                     await owner.GetAsync(Awards),
                     await owner.PutAsJsonAsync(Settings, new { challenge = new { target = 1, windowDays = 30 } }),
                 })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await response.Content.ReadAsStringAsync()).Should().Contain("FEATURE_DISABLED");
        }
        var settings = await PtTestGym.DataAsync(owner, Settings);
        settings.GetProperty("challenge").ValueKind.Should().Be(JsonValueKind.Null, "staff web hides the challenge settings");
        (await PtTestGym.DataAsync(owner, Summary)).GetProperty("challengeEnabled").GetBoolean().Should().BeFalse();
    }

    // ------------------------------------------------------------------ helpers

    private async Task<(HttpClient Coach, HttpClient Trainee, HttpClient Owner)> StartAsync(bool challengeOn = true, int? joinedDaysAgo = null)
    {
        _gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);
        await AddDeskAndJoinDateAsync(joinedDaysAgo);
        await SetFlagAsync(FeatureKeys.PtOwnerDashboard, true);
        if (challengeOn)
            await SetFlagAsync(FeatureKeys.MemFirstMonthChallenge, true);

        var desk = Client(CreateStaffClient(_deskId, _gymId));
        var assigned = await desk.PostAsJsonAsync("api/v1/billing/packages/quick-assign", new
        {
            gymId = _gymId, trainerId = _coachId, memberId = _traineeId, sessionCount = 4, assignedBy = _deskId, isTrial = true,
        });
        assigned.StatusCode.Should().Be(HttpStatusCode.Created, await assigned.Content.ReadAsStringAsync());
        return (Client(CreateTrainerClient(_coachId, _gymId)), Client(CreateTraineeClient(_traineeId, _gymId)),
            Client(CreateGymOwnerClient(Guid.NewGuid(), _gymId)));
    }

    /// <summary>The desk audience for the alert, and (optionally) a roster join date in the past.</summary>
    private async Task AddDeskAndJoinDateAsync(int? joinedDaysAgo)
    {
        using var scope = Factory.Services.CreateScope();
        var gymsDb = scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        gymsDb.StaffRoles.Add(StaffRole.Create(_gymId, _deskId, StaffRoleType.Receptionist));
        if (joinedDaysAgo is { } days)
        {
            var member = GymMember.Create(_gymId, _traineeId, $"M-{Random.Shared.Next(1000, 9999)}", null);
            gymsDb.GymMembers.Add(member);
            gymsDb.Entry(member).Property(m => m.JoinedAt).CurrentValue = DateTime.UtcNow.AddDays(-days);
        }
        await gymsDb.SaveChangesAsync();
    }

    private HttpClient Client(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());
        return client;
    }

    private static object Day(string title) => new { title, exercises = new object[] { new { name = "Squat", sets = 3, reps = "10" } } };

    private async Task<JsonElement> AssignAsync(HttpClient coach)
    {
        var response = await coach.PostAsJsonAsync("api/v1/training/plans", new
        {
            traineeId = _traineeId,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow),
            content = new
            {
                name = "First month",
                goal = "GeneralFitness",
                document = new { schemaVersion = 1, weeks = new[] { new { week = 1, days = new[] { Day("A"), Day("B"), Day("C") } } } },
            },
        });
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        return JsonDocument.Parse(text).RootElement.GetProperty("data").Clone();
    }

    private static Guid PlanId(JsonElement plan) => plan.GetProperty("summary").GetProperty("id").GetGuid();

    private static List<string> DayKeys(JsonElement plan) =>
        plan.GetProperty("document").GetProperty("weeks")[0].GetProperty("days").EnumerateArray()
            .Select(d => d.GetProperty("key").GetString()!).ToList();

    /// <summary>Uploads a finished workout now; returns the sync outcome (created, duplicate, …).</summary>
    private static async Task<string?> WorkOutAsync(HttpClient trainee, Guid planId, string dayKey)
    {
        var response = await trainee.PutAsJsonAsync($"api/v1/training/me/plan-workouts/{Guid.NewGuid()}", new
        {
            planId, dayKey, completedAt = DateTime.UtcNow, sets = Array.Empty<object>(),
        });
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        return JsonDocument.Parse(text).RootElement.GetProperty("data").GetProperty("outcome").GetString();
    }

    private async Task SetFlagAsync(string key, bool enabled)
    {
        using var scope = Factory.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IFeatureFlagAdminService>();
        (await admin.SetOverrideAsync(_gymId, new FlagOverrideChange(key, enabled, "test"), new AdminActor(Guid.NewGuid(), "test")))
            .IsSuccess.Should().BeTrue();
    }

    private static async Task<int> CountNotificationsAsync(HttpClient client)
    {
        var data = await PtTestGym.DataAsync(client, "api/v1/communication/notifications?page=1&pageSize=100");
        var items = data.ValueKind == JsonValueKind.Array ? data : data.GetProperty("items");
        return items.EnumerateArray().Count(n =>
            n.GetProperty("data").TryGetProperty("type", out var t) && t.GetString() == "first_month_challenge_reached");
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
