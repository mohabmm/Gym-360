using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Modules.Scheduling.Infrastructure.Persistence;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Pt;

/// <summary>
/// The whole PT journey for a pt_module gym over HTTP, with integration events delivered by
/// the real outbox processor (polled):
/// desk assigns a 2-session trial with a coach → coach quick-logs session 1 and records
/// measurements → trainee confirms and sees progress → last-trial-session alert → coach logs
/// session 2 → trial shows as "not converted" → trainee buys a 4-session package (list clears)
/// → 3-left and 1-left reminders → owner dashboard shows the right numbers.
/// </summary>
public sealed class PtModuleEndToEndTests : BaseIntegrationTest
{
    private static readonly TimeSpan OutboxTimeout = TimeSpan.FromSeconds(60);

    private Guid _gymId;
    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _traineeId = Guid.NewGuid();
    private readonly Guid _deskId = Guid.NewGuid();

    public PtModuleEndToEndTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Trial_to_paid_journey_works_end_to_end()
    {
        await RegisterPtGymAsync();
        var desk = CreateStaffClient(_deskId, _gymId);
        var coach = CreateTrainerClient(_coachId, _gymId);
        var trainee = CreateTraineeClient(_traineeId, _gymId);
        var owner = CreateGymOwnerClient(Guid.NewGuid(), _gymId);
        foreach (var c in new[] { desk, coach, trainee, owner })
            c.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());

        // 1. Desk assigns a free 2-session trial with the coach.
        (await QuickAssignAsync(desk, sessions: 2, isTrial: true)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await QuickAssignAsync(desk, sessions: 1, isTrial: true)).StatusCode
            .Should().Be(HttpStatusCode.Conflict, "one trial per trainee");
        (await QuickAssignAsync(desk, sessions: 8, isTrial: false, price: 4000m)).StatusCode
            .Should().Be(HttpStatusCode.Conflict, "one coach at a time: the trial still has sessions left");

        // 2. Coach logs session 1 with one tap, and records measurements from session 1.
        var first = await QuickLogAsync(coach);
        first.GetProperty("remainingSessions").GetInt32().Should().Be(1);
        (await coach.PostAsJsonAsync("api/v1/progress/body-metrics/client", new
        {
            clientId = _traineeId,
            gymId = _gymId,
            date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            weight = 92.5m,
            weightUnit = "kg",
            bodyFatPercentage = 24m,
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        // 3. Trainee sees the session waiting for confirmation, confirms it, and sees progress.
        var sessionId = first.GetProperty("sessionId").GetGuid();
        var pending = await DataAsync(trainee, $"api/v1/scheduling/sessions?memberId={_traineeId}&memberConfirmation=Pending&page=1&pageSize=20");
        pending.GetProperty("items").EnumerateArray().Select(s => s.GetProperty("id").GetGuid()).Should().Contain(sessionId);
        (await trainee.PostAsync($"api/v1/scheduling/sessions/{sessionId}/member-confirm", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        var metrics = await DataAsync(trainee, $"api/v1/progress/body-metrics?gymId={_gymId}");
        metrics.EnumerateArray().Should().Contain(m => m.GetProperty("weight").GetDecimal() == 92.5m);

        // 4. Last free session alert reaches the coach.
        await EventuallyAsync(() => HasNotificationAsync(coach, "trial_last_session"), "coach gets the last-trial-session alert");

        // 5. Coach logs session 2 → trial finished, not converted.
        await AgePreviousQuickLogsAsync();
        (await QuickLogAsync(coach)).GetProperty("remainingSessions").GetInt32().Should().Be(0);
        await EventuallyAsync(async () => (await NotConvertedAsync(desk)).Contains(_traineeId), "trial appears as not converted");

        // 6. Trainee buys a 4-session package from the desk → no longer "not converted".
        (await QuickAssignAsync(desk, sessions: 4, isTrial: false, price: 2000m)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await NotConvertedAsync(desk)).Should().NotContain(_traineeId);

        // 7. Coach logs 3 sessions: 3 left and 1 left reminders reach the trainee.
        for (var i = 0; i < 3; i++)
        {
            await AgePreviousQuickLogsAsync();
            await QuickLogAsync(coach);
        }

        await EventuallyAsync(() => HasNotificationAsync(trainee, "renewal_3_left"), "trainee gets the 3-left reminder");
        await EventuallyAsync(() => HasNotificationAsync(trainee, "renewal_1_left"), "trainee gets the 1-left reminder");
        (await HasNotificationAsync(coach, "renewal_1_left")).Should().BeTrue("the coach hears about 1 left too");

        // 8. Owner dashboard: 1 trial finished and converted (100%), 1 new paid package, 5 sessions.
        await EventuallyAsync(async () => (await CoachRowAsync(owner)).GetProperty("sessionsLogged").GetInt32() == 5,
            "all 5 sessions are counted");
        var row = await CoachRowAsync(owner);
        row.GetProperty("trialsFinished").GetInt32().Should().Be(1);
        row.GetProperty("trialsConverted").GetInt32().Should().Be(1);
        row.GetProperty("conversionRate").GetDecimal().Should().Be(100m);
        row.GetProperty("newPaidPackages").GetInt32().Should().Be(1);
        row.GetProperty("renewals").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Disputed_session_is_reversed_by_the_desk_and_the_session_is_given_back()
    {
        await RegisterPtGymAsync();
        var desk = CreateStaffClient(_deskId, _gymId);
        var coach = CreateTrainerClient(_coachId, _gymId);
        var trainee = CreateTraineeClient(_traineeId, _gymId);
        foreach (var c in new[] { desk, coach, trainee })
            c.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());

        (await QuickAssignAsync(desk, sessions: 4, isTrial: false, price: 2000m)).EnsureSuccessStatusCode();
        var sessionId = (await QuickLogAsync(coach)).GetProperty("sessionId").GetGuid();
        await EventuallyAsync(async () => await RemainingAsync(trainee) == 3, "session is deducted");

        (await trainee.PostAsJsonAsync($"api/v1/scheduling/sessions/{sessionId}/member-dispute", new { reason = "Not me" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var review = await DataAsync(desk, "api/v1/scheduling/sessions/review");
        review.EnumerateArray().Select(s => s.GetProperty("id").GetGuid()).Should().Contain(sessionId);

        (await desk.PostAsync($"api/v1/scheduling/sessions/{sessionId}/reverse", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        await EventuallyAsync(async () => await RemainingAsync(trainee) == 4, "the package session is restored");
    }

    // ------------------------------------------------------------------ helpers

    private async Task RegisterPtGymAsync() =>
        _gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);

    private Task<HttpResponseMessage> QuickAssignAsync(HttpClient desk, int sessions, bool isTrial, decimal? price = null) =>
        desk.PostAsJsonAsync("api/v1/billing/packages/quick-assign", new
        {
            gymId = _gymId,
            trainerId = _coachId,
            memberId = _traineeId,
            sessionCount = sessions,
            assignedBy = _deskId,
            totalPrice = price,
            paymentMethod = price is > 0 ? "Cash" : null,
            expiresAt = DateTime.UtcNow.AddDays(-1), // ignored while pt.auto_expiry is off
            isTrial,
        });

    private async Task<JsonElement> QuickLogAsync(HttpClient coach)
    {
        var response = await coach.PostAsJsonAsync("api/v1/scheduling/sessions/quick-log", new { memberId = _traineeId });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var data = JsonDocument.Parse(body).RootElement.GetProperty("data").Clone();
        data.GetProperty("alreadyLogged").GetBoolean().Should().BeFalse();
        return data;
    }

    /// <summary>Moves earlier quick-logs out of the 5-minute double-tap window (simulates time passing).</summary>
    private async Task AgePreviousQuickLogsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [scheduling].[sessions] SET CreatedAt = DATEADD(minute, -10, CreatedAt) WHERE TrainerId = {_coachId} AND MemberId = {_traineeId}");
    }

    private static Task<JsonElement> DataAsync(HttpClient client, string url) => PtTestGym.DataAsync(client, url);

    private async Task<HashSet<Guid>> NotConvertedAsync(HttpClient desk)
    {
        var data = await DataAsync(desk, "api/v1/billing/trials/not-converted");
        return data.EnumerateArray().Select(t => t.GetProperty("memberId").GetGuid()).ToHashSet();
    }

    private async Task<int> RemainingAsync(HttpClient trainee)
    {
        var data = await DataAsync(trainee, "api/v1/billing/me/packages");
        var items = data.ValueKind == JsonValueKind.Array ? data : data.GetProperty("items");
        return items.EnumerateArray()
            .Where(p => p.TryGetProperty("status", out var s) && s.GetString() == "Active")
            .Sum(p => p.GetProperty("remainingSessions").GetInt32());
    }

    private static async Task<bool> HasNotificationAsync(HttpClient client, string type)
    {
        var data = await DataAsync(client, "api/v1/communication/notifications?page=1&pageSize=100");
        var items = data.ValueKind == JsonValueKind.Array ? data : data.GetProperty("items");
        return items.EnumerateArray().Any(n =>
            n.GetProperty("data").TryGetProperty("type", out var t) && t.GetString() == type);
    }

    private async Task<JsonElement> CoachRowAsync(HttpClient owner)
    {
        var from = Uri.EscapeDataString(DateTime.UtcNow.AddDays(-1).ToString("O"));
        var to = Uri.EscapeDataString(DateTime.UtcNow.AddDays(1).ToString("O"));
        var data = await DataAsync(owner, $"api/v1/billing/pt-dashboard?from={from}&to={to}");
        return data.GetProperty("coaches").EnumerateArray().Single(c => c.GetProperty("trainerId").GetGuid() == _coachId);
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
