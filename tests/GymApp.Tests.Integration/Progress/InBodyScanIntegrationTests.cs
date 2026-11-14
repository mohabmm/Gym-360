using System.Net;
using System.Net.Http.Json;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Infrastructure.Tenancy.Provisioning;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using GymApp.Tests.Integration.Abstractions;
using GymApp.Tests.Integration.Pt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Progress;

/// <summary>
/// progress.inbody over HTTP: on a Business gym the member saves an InBody scan and sees height,
/// muscle and water (with derived %) on their journey; a second scan the same day updates the
/// entry; a pt_module gym is refused; the schema patch adds the columns to old gym schemas.
/// </summary>
public sealed class InBodyScanIntegrationTests : BaseIntegrationTest
{
    private const string Url = "api/v1/progress/body-metrics/inbody";

    private readonly Guid _traineeId = Guid.NewGuid();

    public InBodyScanIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private static string Today => DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

    private static object Scan(decimal weightKg) => new
    {
        date = Today,
        weightKg,
        heightCm = 178m,
        bodyFatPercentage = 18.5m,
        skeletalMuscleMassKg = 35m,
        bodyWaterKg = 45m,
    };

    [Fact]
    public async Task Business_gym_member_saves_a_scan_and_a_second_scan_the_same_day_updates_it()
    {
        var gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId, FeatureRegistry.BusinessPreset);
        var trainee = WithGym(CreateTraineeClient(_traineeId, gymId), gymId);

        // Tape measurement logged earlier today must survive the scan.
        (await trainee.PostAsJsonAsync("api/v1/progress/body-metrics", new
        {
            date = Today, weight = 81m, weightUnit = "kg", measurements = new { waist = 90m, measurementUnit = "Cm" },
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var first = await trainee.PostAsJsonAsync(Url, Scan(80m));
        first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());

        var second = await trainee.PostAsJsonAsync(Url, Scan(79.6m));
        second.StatusCode.Should().Be(HttpStatusCode.OK, await second.Content.ReadAsStringAsync());

        var journey = await PtTestGym.DataAsync(trainee, "api/v1/progress/journey");
        var entry = journey.GetProperty("entries").EnumerateArray().Should().ContainSingle().Subject;
        entry.GetProperty("source").GetString().Should().Be("InBodyScan");
        entry.GetProperty("weight").GetDecimal().Should().Be(79.6m);
        entry.GetProperty("bodyFatPercentage").GetDecimal().Should().Be(18.5m);
        entry.GetProperty("measurements").GetProperty("waist").GetDecimal().Should().Be(90m);

        var composition = entry.GetProperty("composition");
        composition.GetProperty("heightCm").GetDecimal().Should().Be(178m);
        composition.GetProperty("skeletalMuscleMassKg").GetDecimal().Should().Be(35m);
        composition.GetProperty("bodyWaterKg").GetDecimal().Should().Be(45m);
        composition.GetProperty("skeletalMusclePercent").GetDecimal().Should().Be(44.0m);
        composition.GetProperty("bodyWaterPercent").GetDecimal().Should().Be(56.5m);
        composition.GetProperty("bmi").GetDecimal().Should().Be(25.1m);
    }

    [Fact]
    public async Task First_scan_of_the_day_is_created()
    {
        var gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId, FeatureRegistry.BusinessPreset);
        var trainee = WithGym(CreateTraineeClient(_traineeId, gymId), gymId);

        var response = await trainee.PostAsJsonAsync(Url, Scan(80m));

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Pt_module_gym_is_refused()
    {
        var gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId);
        var trainee = WithGym(CreateTraineeClient(_traineeId, gymId), gymId);

        var response = await trainee.PostAsJsonAsync(Url, Scan(80m));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().Contain(FeatureKeys.ProgressInBody);
    }

    [Fact]
    public async Task Impossible_values_are_rejected()
    {
        var gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId, FeatureRegistry.BusinessPreset);
        var trainee = WithGym(CreateTraineeClient(_traineeId, gymId), gymId);

        var response = await trainee.PostAsJsonAsync(Url, new { date = Today, weightKg = 80m, bodyWaterKg = 95m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unauthenticated_is_refused()
    {
        (await HttpClient.PostAsJsonAsync(Url, Scan(80m))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Schema_patch_adds_composition_columns_to_old_gym_schemas_idempotently()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantRegistryDbContext>().Database;
        var schema = $"g{Random.Shared.Next(900000, 999999)}_progress";

        await db.ExecuteSqlRawAsync($@"
EXEC('CREATE SCHEMA [{schema}]');
CREATE TABLE [{schema}].[body_metric_entries] (Id uniqueidentifier NOT NULL PRIMARY KEY, Weight decimal(8,2) NULL);
INSERT INTO [{schema}].[body_metric_entries] VALUES (NEWID(), 80);");

        await db.ExecuteSqlRawAsync(BodyCompositionSchemaPatch.Sql);
        await db.ExecuteSqlRawAsync(BodyCompositionSchemaPatch.Sql); // second deploy: no-op

        var columns = await db.SqlQueryRaw<string>(
            "SELECT COLUMN_NAME AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = {0} AND TABLE_NAME = 'body_metric_entries'",
            schema).ToListAsync();
        columns.Should().BeEquivalentTo(
            "Id", "Weight", "composition_height_cm", "composition_skeletal_muscle_kg", "composition_body_water_kg");
        (await db.SqlQueryRaw<decimal>($"SELECT Weight AS Value FROM [{schema}].[body_metric_entries]").SingleAsync())
            .Should().Be(80m);
    }

    private static HttpClient WithGym(HttpClient client, Guid gymId)
    {
        client.DefaultRequestHeaders.Add("X-Gym-Id", gymId.ToString());
        return client;
    }
}
