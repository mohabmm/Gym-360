using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Common.Infrastructure.Tenancy.Provisioning;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Pt;

/// <summary>
/// pt.progress_measurements over HTTP: the coach measures on day 1 and day 30 and sets the goal,
/// the member sees every entry since day one with the goal, a member can't pass a self entry off
/// as coach-measured, and a coach without a package with the member is refused.
/// </summary>
public sealed class ProgressJourneyIntegrationTests : BaseIntegrationTest
{
    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _otherCoachId = Guid.NewGuid();
    private readonly Guid _traineeId = Guid.NewGuid();
    private readonly Guid _deskId = Guid.NewGuid();
    private Guid _gymId;

    public ProgressJourneyIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Member_and_coach_see_progress_since_day_one_and_other_coaches_are_refused()
    {
        _gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);
        var desk = WithGym(CreateStaffClient(_deskId, _gymId));
        var coach = WithGym(CreateTrainerClient(_coachId, _gymId));
        var otherCoach = WithGym(CreateTrainerClient(_otherCoachId, _gymId));
        var trainee = WithGym(CreateTraineeClient(_traineeId, _gymId));

        (await desk.PostAsJsonAsync("api/v1/billing/packages/quick-assign", new
        {
            gymId = _gymId, trainerId = _coachId, memberId = _traineeId,
            sessionCount = 2, assignedBy = _deskId, isTrial = true,
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        // Coach: day 1 and day 30 measurements, goal = lose fat to 80 kg.
        await CoachMeasuresAsync(coach, daysAgo: 30, weight: 95m, waist: 102m);
        await CoachMeasuresAsync(coach, daysAgo: 0, weight: 91m, waist: 97m);
        (await coach.PutAsJsonAsync($"api/v1/progress/clients/{_traineeId}/goal", new { goal = "LoseFat", targetWeight = 80m }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Member logs their own weight and claims it was coach-measured: stored as self-reported.
        (await trainee.PostAsJsonAsync("api/v1/progress/body-metrics", new
        {
            date = Day(15), weight = 93m, weightUnit = "kg", source = "TrainerMeasured",
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var journey = await PtTestGym.DataAsync(trainee, "api/v1/progress/journey");
        var entries = journey.GetProperty("entries").EnumerateArray().ToList();
        entries.Select(e => e.GetProperty("weight").GetDecimal()).Should().Equal(95m, 93m, 91m);
        entries.Select(e => e.GetProperty("source").GetString()).Should().Equal("TrainerMeasured", "SelfReported", "TrainerMeasured");
        entries[0].GetProperty("measurements").GetProperty("waist").GetDecimal().Should().Be(102m);
        journey.GetProperty("goal").GetProperty("goal").GetString().Should().Be("LoseFat");
        journey.GetProperty("goal").GetProperty("targetWeight").GetDecimal().Should().Be(80m);

        // The coach reads the same journey for their client.
        var coachView = await PtTestGym.DataAsync(coach, $"api/v1/progress/clients/{_traineeId}/journey");
        coachView.GetProperty("entries").GetArrayLength().Should().Be(3);

        // A coach the member has no package with is refused everywhere.
        (await otherCoach.GetAsync($"api/v1/progress/clients/{_traineeId}/journey")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await otherCoach.GetAsync($"api/v1/progress/clients/{_traineeId}?gymId={_gymId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await otherCoach.PutAsJsonAsync($"api/v1/progress/clients/{_traineeId}/goal", new { goal = "BuildMuscle" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The member changes their own goal.
        (await trainee.PutAsJsonAsync("api/v1/progress/goal", new { goal = "BuildMuscle" })).StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await PtTestGym.DataAsync(trainee, "api/v1/progress/journey");
        updated.GetProperty("goal").GetProperty("goal").GetString().Should().Be("BuildMuscle");
        updated.GetProperty("goal").GetProperty("setBy").GetGuid().Should().Be(_traineeId);
    }

    [Fact]
    public async Task Schema_patch_creates_progress_goals_in_old_gym_schemas_idempotently()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantRegistryDbContext>().Database;
        var schema = $"g{Random.Shared.Next(900000, 999999)}_progress";

        await db.ExecuteSqlRawAsync($@"
EXEC('CREATE SCHEMA [{schema}]');
CREATE TABLE [{schema}].[body_metric_entries] (Id uniqueidentifier NOT NULL PRIMARY KEY);
INSERT INTO [{schema}].[body_metric_entries] VALUES (NEWID());");

        await db.ExecuteSqlRawAsync(ProgressGoalsSchemaPatch.Sql);
        await db.ExecuteSqlRawAsync(ProgressGoalsSchemaPatch.Sql); // second deploy: no-op

        var columns = await db.SqlQueryRaw<string>(
            "SELECT COLUMN_NAME AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = {0} AND TABLE_NAME = 'progress_goals'",
            schema).ToListAsync();
        columns.Should().BeEquivalentTo("Id", "UserId", "GymId", "Goal", "TargetWeight", "SetBy", "UpdatedAt");
        (await db.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM [{schema}].[body_metric_entries]").SingleAsync())
            .Should().Be(1);
    }

    private HttpClient WithGym(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());
        return client;
    }

    private static string Day(int daysAgo) =>
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-daysAgo)).ToString("yyyy-MM-dd");

    private async Task CoachMeasuresAsync(HttpClient coach, int daysAgo, decimal weight, decimal waist)
    {
        var response = await coach.PostAsJsonAsync("api/v1/progress/body-metrics/client", new
        {
            clientId = _traineeId,
            gymId = _gymId,
            date = Day(daysAgo),
            weight,
            weightUnit = "kg",
            measurements = new { waist, measurementUnit = "Cm" },
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }
}
