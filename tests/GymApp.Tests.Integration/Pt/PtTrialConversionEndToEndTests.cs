using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using GymApp.Modules.Billing.Application.Commands.TrialFollowUps;
using GymApp.Modules.Billing.Infrastructure.Data;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Infrastructure.Data;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Pt;

/// <summary>
/// Trial → paid conversion over HTTP, with integration events delivered by the real outbox:
/// trial finished (trainee nudged) → coach sends the plan (trainee told) → trainee taps
/// "I'm interested" (desk + coach told, idempotent) → desk sells the package → the lead shows
/// as converted. Plus the day-3 follow-up job, which fires once.
/// </summary>
public sealed class PtTrialConversionEndToEndTests : BaseIntegrationTest
{
    private static readonly TimeSpan OutboxTimeout = TimeSpan.FromSeconds(60);

    private Guid _gymId;
    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _traineeId = Guid.NewGuid();
    private readonly Guid _deskId = Guid.NewGuid();

    public PtTrialConversionEndToEndTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Trial_recap_and_interest_lead_convert_the_trainee()
    {
        var (desk, coach, trainee) = await StartFinishedTrialAsync();
        await EventuallyAsync(() => HasNotificationAsync(trainee, "trial_finished"), "trainee is nudged when the trial ends");

        // Coach sends the plan; the trainee is told and can read it.
        var trialId = await TrialIdAsync(trainee);
        (await coach.PostAsJsonAsync($"api/v1/billing/trials/{trialId}/recap", new { note = "Great start, 3x a week", recommendedSessions = 12 }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        await EventuallyAsync(() => HasNotificationAsync(trainee, "trial_recap"), "trainee is told the plan is ready");
        var recap = await DataAsync(trainee, "api/v1/billing/trials/my-recap");
        recap.GetProperty("recommendedSessions").GetInt32().Should().Be(12);
        recap.GetProperty("trialFinished").GetBoolean().Should().BeTrue();

        // Trainee taps "I'm interested" twice: one lead; desk and coach are told.
        var first = await PostDataAsync(trainee, "api/v1/billing/package-interest", new { sourcePackageId = trialId });
        first.GetProperty("alreadyPending").GetBoolean().Should().BeFalse();
        var second = await PostDataAsync(trainee, "api/v1/billing/package-interest", new { sourcePackageId = trialId });
        second.GetProperty("alreadyPending").GetBoolean().Should().BeTrue();
        second.GetProperty("interestId").GetGuid().Should().Be(first.GetProperty("interestId").GetGuid());
        await EventuallyAsync(() => HasNotificationAsync(desk, "package_interest"), "desk gets the lead");
        (await HasNotificationAsync(coach, "package_interest")).Should().BeTrue("the coach hears about the lead too");

        var pending = await DataAsync(desk, "api/v1/billing/package-interest?state=pending");
        var lead = pending.EnumerateArray().Single(l => l.GetProperty("memberId").GetGuid() == _traineeId);
        lead.GetProperty("source").GetString().Should().Be("TrialRecap");
        lead.GetProperty("recommendedSessions").GetInt32().Should().Be(12);

        // Desk sells the 12-session package: the lead is converted, nothing left pending.
        (await QuickAssignAsync(desk, sessions: 12, isTrial: false, price: 6000m)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await DataAsync(desk, "api/v1/billing/package-interest?state=pending")).EnumerateArray()
            .Should().NotContain(l => l.GetProperty("memberId").GetGuid() == _traineeId);
        (await DataAsync(desk, "api/v1/billing/package-interest?state=converted")).EnumerateArray()
            .Should().Contain(l => l.GetProperty("memberId").GetGuid() == _traineeId);
    }

    [Fact]
    public async Task Day_three_follow_up_reaches_the_trainee_once()
    {
        var (_, _, trainee) = await StartFinishedTrialAsync();
        await AgeTrialAsync(days: 4);

        (await Sender.Send(new SendTrialFollowUpsCommand())).Value.Should().BeGreaterThanOrEqualTo(1);
        await EventuallyAsync(() => HasNotificationAsync(trainee, "trial_followup_day3"), "trainee gets the day-3 reminder");

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var trial = await db.SessionPackages.AsNoTracking().SingleAsync(p => p.MemberId == _traineeId && p.IsTrial);
        (await Sender.Send(new SendTrialFollowUpsCommand())).IsSuccess.Should().BeTrue();
        (await db.SessionPackages.AsNoTracking().SingleAsync(p => p.Id == trial.Id)).MilestonesSent
            .Should().Be(trial.MilestonesSent, "the day-3 reminder is never sent twice");
    }

    [Fact]
    public async Task Only_the_trial_coach_can_send_the_plan()
    {
        var (_, _, trainee) = await StartFinishedTrialAsync();
        var otherCoach = CreateTrainerClient(Guid.NewGuid(), _gymId);
        otherCoach.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());

        var response = await otherCoach.PostAsJsonAsync(
            $"api/v1/billing/trials/{await TrialIdAsync(trainee)}/recap", new { note = "Plan", recommendedSessions = 8 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------ helpers

    private async Task<(HttpClient Desk, HttpClient Coach, HttpClient Trainee)> StartFinishedTrialAsync()
    {
        await RegisterPtGymAsync();
        var desk = CreateStaffClient(_deskId, _gymId);
        var coach = CreateTrainerClient(_coachId, _gymId);
        var trainee = CreateTraineeClient(_traineeId, _gymId);
        foreach (var c in new[] { desk, coach, trainee })
            c.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());

        (await QuickAssignAsync(desk, sessions: 1, isTrial: true)).StatusCode.Should().Be(HttpStatusCode.Created);
        var log = await coach.PostAsJsonAsync("api/v1/scheduling/sessions/quick-log", new { memberId = _traineeId });
        log.StatusCode.Should().Be(HttpStatusCode.OK, await log.Content.ReadAsStringAsync());
        await EventuallyAsync(async () => await TrialStatusAsync(trainee) == "Exhausted", "the only free session is used");
        return (desk, coach, trainee);
    }

    private async Task RegisterPtGymAsync()
    {
        using var scope = Factory.Services.CreateScope();

        var gymsDb = scope.ServiceProvider.GetRequiredService<GymsDbContext>();
        var gym = Gym.Create(
            Guid.NewGuid(),
            new GymDetails
            {
                Name = "PT Conversion Gym",
                Description = null,
                Address = new Address("Street", "Cairo", "Cairo", "Egypt", "11111"),
                ContactInfo = new ContactInfo("conversion@test.com", "+201234567891"),
                GymCode = GymCode.Generate(),
            });
        gym.Approve();
        gym.AddMember(new MemberId(_traineeId));
        gymsDb.Gyms.Add(gym);
        gymsDb.StaffRoles.Add(StaffRole.Create(gym.Id, _deskId, StaffRoleType.Receptionist)); // desk recipients
        await gymsDb.SaveChangesAsync();
        _gymId = gym.Id;

        var registry = scope.ServiceProvider.GetRequiredService<TenantRegistryDbContext>();
        var suffix = _gymId.ToString("N")[..8];
        registry.Gyms.Add(new GymRegistration
        {
            GymId = _gymId,
            OwnerId = Guid.NewGuid(),
            Name = $"PT Conversion {suffix}",
            Slug = $"ptc-{suffix}",
            SchemaPrefix = $"g{Random.Shared.Next(100000, 999999)}",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
        });
        await registry.SaveChangesAsync();

        var admin = scope.ServiceProvider.GetRequiredService<IFeatureFlagAdminService>();
        (await admin.SetGymPresetAsync(new[] { _gymId }, FeatureRegistry.PtModulePreset, new AdminActor(Guid.NewGuid(), "test")))
            .IsSuccess.Should().BeTrue();
    }

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
            isTrial,
        });

    /// <summary>Moves the trainee's finished trial <paramref name="days"/> days into the past.</summary>
    private async Task AgeTrialAsync(int days)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        await db.SessionPackages
            .Where(p => p.MemberId == _traineeId && p.IsTrial)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.ExhaustedAt, p => p.ExhaustedAt!.Value.AddDays(-days)));
    }

    private async Task<JsonElement> TrialAsync(HttpClient trainee)
    {
        var data = await DataAsync(trainee, "api/v1/billing/me/packages?status=All");
        var items = data.ValueKind == JsonValueKind.Array ? data : data.GetProperty("items");
        return items.EnumerateArray().Single(p => p.GetProperty("isTrial").GetBoolean());
    }

    private async Task<Guid> TrialIdAsync(HttpClient trainee) => (await TrialAsync(trainee)).GetProperty("id").GetGuid();

    private async Task<string?> TrialStatusAsync(HttpClient trainee) => (await TrialAsync(trainee)).GetProperty("status").GetString();

    private static async Task<JsonElement> DataAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue($"{url} → {(int)response.StatusCode} {body}");
        return JsonDocument.Parse(body).RootElement.GetProperty("data").Clone();
    }

    private static async Task<JsonElement> PostDataAsync(HttpClient client, string url, object payload)
    {
        var response = await client.PostAsJsonAsync(url, payload);
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue($"{url} → {(int)response.StatusCode} {body}");
        return JsonDocument.Parse(body).RootElement.GetProperty("data").Clone();
    }

    private static async Task<bool> HasNotificationAsync(HttpClient client, string type)
    {
        var data = await DataAsync(client, "api/v1/communication/notifications?page=1&pageSize=100");
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
