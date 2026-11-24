using GymApp.Common.Infrastructure.Tenancy.Provisioning;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Pt;

/// <summary>
/// Existing per-gym training schemas are never EF-migrated, so the startup patch must give them
/// the training-plan tables (without them a coach cannot save a plan and the member cannot load
/// one), add the challenge columns to an older settings table, keep its rows, and be a no-op on
/// the next deploy.
/// </summary>
public sealed class TrainingPlanSchemaPatchTests : BaseIntegrationTest
{
    public TrainingPlanSchemaPatchTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Creates_plan_tables_in_old_gym_schema_idempotently_and_keeps_settings()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantRegistryDbContext>().Database;
        var schema = $"g{Random.Shared.Next(900000, 999999)}_training";

        // An "old" gym: training tables from before plans, and a settings table from before the challenge.
        await db.ExecuteSqlRawAsync($@"
EXEC('CREATE SCHEMA [{schema}]');
CREATE TABLE [{schema}].[workout_assignments] (Id uniqueidentifier NOT NULL PRIMARY KEY);
CREATE TABLE [{schema}].[training_plan_settings] (Id uniqueidentifier NOT NULL PRIMARY KEY, GymId uniqueidentifier NOT NULL, InactiveDays int NOT NULL, UpdatedAt datetime2 NOT NULL, UpdatedBy uniqueidentifier NULL);
INSERT INTO [{schema}].[training_plan_settings] VALUES (NEWID(), NEWID(), 7, SYSUTCDATETIME(), NULL);");

        await db.ExecuteSqlRawAsync(TrainingPlanSchemaPatch.Sql);
        await db.ExecuteSqlRawAsync(TrainingPlanSchemaPatch.Sql); // second deploy: no-op

        (await ColumnsAsync(db, schema, "plan_templates"))
            .Should().Contain(new[] { "Id", "GymId", "Scope", "OwnerTrainerId", "Name", "NameAr", "Goal", "document", "SampleKey", "IsArchived" });
        (await ColumnsAsync(db, schema, "training_plans"))
            .Should().Contain(new[] { "Id", "TraineeId", "TrainerId", "SourceTemplateId", "document", "StartDate", "Status", "Version", "EndedAt" });
        (await ColumnsAsync(db, schema, "plan_workout_completions"))
            .Should().Contain(new[] { "PlanId", "ClientId", "DayKey", "SequenceIndex", "sets", "IsVoided" });
        (await ColumnsAsync(db, schema, "challenge_awards"))
            .Should().Contain(new[] { "TraineeId", "WindowStart", "Target", "RewardTextAr", "DeliveredAt" });
        (await ColumnsAsync(db, schema, "training_plan_settings"))
            .Should().Contain(new[] { "InactiveDays", "ChallengeTarget", "ChallengeWindowDays", "RewardTextAr", "RewardTextEn" });

        (await ScalarAsync(db, $"SELECT COUNT(*) AS Value FROM [{schema}].[training_plan_settings] WHERE InactiveDays = 7 AND ChallengeTarget = 12 AND ChallengeWindowDays = 30"))
            .Should().Be(1);
        (await ScalarAsync(db, $"SELECT COUNT(*) AS Value FROM sys.indexes WHERE name = 'UX_training_plans_active_trainee' AND object_id = OBJECT_ID('{schema}.training_plans') AND has_filter = 1"))
            .Should().Be(1);
    }

    private static Task<List<string>> ColumnsAsync(DatabaseFacade db, string schema, string table) =>
        db.SqlQueryRaw<string>(
            "SELECT COLUMN_NAME AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = {0} AND TABLE_NAME = {1}",
            schema, table).ToListAsync();

    private static Task<int> ScalarAsync(DatabaseFacade db, string sql) =>
        db.SqlQueryRaw<int>(sql).SingleAsync();
}
