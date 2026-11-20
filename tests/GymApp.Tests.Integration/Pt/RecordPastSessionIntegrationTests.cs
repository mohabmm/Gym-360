using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Pt;

/// <summary>
/// pt.dual_session_logging over HTTP: the coach records when a session happened — today or up to
/// 7 days back. The package is deducted, the same slot is never counted twice, older is refused.
/// </summary>
public sealed class RecordPastSessionIntegrationTests : BaseIntegrationTest
{
    private const string QuickLogUrl = "api/v1/scheduling/sessions/quick-log";

    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _traineeId = Guid.NewGuid();
    private readonly Guid _deskId = Guid.NewGuid();

    public RecordPastSessionIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private static DateOnly CairoToday()
    {
        var cairo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cairo));
    }

    [Fact]
    public async Task Coach_records_a_forgotten_session_from_yesterday_once()
    {
        var coach = await ArrangeAsync(sessions: 4);
        var yesterday = CairoToday().AddDays(-1);

        var first = await RecordAsync(coach, yesterday, "18:00:00");
        first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        var data = await DataAsync(first);
        data.GetProperty("alreadyLogged").GetBoolean().Should().BeFalse();
        data.GetProperty("remainingSessions").GetInt32().Should().Be(3);
        data.GetProperty("date").GetString().Should().Be(yesterday.ToString("yyyy-MM-dd"));

        var again = await DataAsync(await RecordAsync(coach, yesterday, "18:00:00"));
        again.GetProperty("alreadyLogged").GetBoolean().Should().BeTrue("the same slot is not counted twice");
        again.GetProperty("sessionId").GetGuid().Should().Be(data.GetProperty("sessionId").GetGuid());

        var other = await DataAsync(await RecordAsync(coach, yesterday.AddDays(-1), "09:00:00"));
        other.GetProperty("alreadyLogged").GetBoolean().Should().BeFalse("a different past session right after still counts");
        other.GetProperty("sessionId").GetGuid().Should().NotBe(data.GetProperty("sessionId").GetGuid());
    }

    [Fact]
    public async Task More_than_seven_days_back_is_refused()
    {
        var coach = await ArrangeAsync(sessions: 2);

        var response = await RecordAsync(coach, CairoToday().AddDays(-8), "10:00:00");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Scheduling.QuickLog.TooOld");
    }

    private async Task<HttpClient> ArrangeAsync(int sessions)
    {
        var gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);
        var desk = CreateStaffClient(_deskId, gymId);
        var coach = CreateTrainerClient(_coachId, gymId);
        foreach (var c in new[] { desk, coach })
            c.DefaultRequestHeaders.Add("X-Gym-Id", gymId.ToString());

        var assigned = await desk.PostAsJsonAsync("api/v1/billing/packages/quick-assign", new
        {
            gymId,
            trainerId = _coachId,
            memberId = _traineeId,
            sessionCount = sessions,
            assignedBy = _deskId,
        });
        assigned.StatusCode.Should().Be(HttpStatusCode.Created, await assigned.Content.ReadAsStringAsync());
        return coach;
    }

    private Task<HttpResponseMessage> RecordAsync(HttpClient coach, DateOnly date, string startTime) =>
        coach.PostAsJsonAsync(QuickLogUrl, new
        {
            memberId = _traineeId,
            date = date.ToString("yyyy-MM-dd"),
            startTime,
            durationMinutes = 60,
        });

    private static async Task<JsonElement> DataAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return JsonDocument.Parse(body).RootElement.GetProperty("data").Clone();
    }
}
