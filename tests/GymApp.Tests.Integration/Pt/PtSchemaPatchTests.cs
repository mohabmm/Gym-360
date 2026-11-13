using GymApp.Common.Infrastructure.Tenancy.Provisioning;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using GymApp.Tests.Integration.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Pt;

/// <summary>
/// The startup patch must add the PT columns and tables to existing per-gym schemas (which are
/// never EF-migrated), keep existing rows, and be safe to run on every deploy.
/// </summary>
public sealed class PtSchemaPatchTests : BaseIntegrationTest
{
    public PtSchemaPatchTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Adds_missing_columns_to_old_per_gym_tables_idempotently_and_keeps_data()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantRegistryDbContext>().Database;
        var prefix = $"g{Random.Shared.Next(900000, 999999)}";

        // An "old" gym schema: tables without the PT columns, with one existing row each.
        await db.ExecuteSqlRawAsync($@"
EXEC('CREATE SCHEMA [{prefix}_billing]');
EXEC('CREATE SCHEMA [{prefix}_scheduling]');
CREATE TABLE [{prefix}_billing].[session_packages] (Id uniqueidentifier NOT NULL PRIMARY KEY, Name nvarchar(50) NOT NULL);
CREATE TABLE [{prefix}_scheduling].[sessions] (Id uniqueidentifier NOT NULL PRIMARY KEY, Focus nvarchar(50) NULL);
INSERT INTO [{prefix}_billing].[session_packages] VALUES (NEWID(), N'old package');
INSERT INTO [{prefix}_scheduling].[sessions] VALUES (NEWID(), N'old session');");

        await db.ExecuteSqlRawAsync(PtSchemaPatch.Sql);
        await db.ExecuteSqlRawAsync(PtSchemaPatch.Sql); // second deploy: no-op

        var packageColumns = await ColumnsAsync(db, $"{prefix}_billing", "session_packages");
        packageColumns.Should().Contain(new[] { "IsTrial", "ExhaustedAt", "MilestonesSent", "RecapNote", "RecommendedSessions", "RecapSentAt", "Name" });
        (await ColumnsAsync(db, $"{prefix}_billing", "package_interests"))
            .Should().Contain(new[] { "Id", "GymId", "MemberId", "TrainerId", "SourcePackageId", "Source", "Status", "CreatedAt" });
        var sessionColumns = await ColumnsAsync(db, $"{prefix}_scheduling", "sessions");
        sessionColumns.Should().Contain(new[] { "IsQuickLog", "MemberConfirmation", "MemberRespondedAt", "DisputeReason", "Focus" });

        (await ScalarAsync(db, $"SELECT COUNT(*) AS Value FROM [{prefix}_billing].[session_packages] WHERE Name = N'old package' AND IsTrial = 0 AND MilestonesSent = 0"))
            .Should().Be(1);
        (await ScalarAsync(db, $"SELECT COUNT(*) AS Value FROM [{prefix}_scheduling].[sessions] WHERE Focus = N'old session' AND MemberConfirmation = 0 AND IsQuickLog = 0"))
            .Should().Be(1);
    }

    private static Task<List<string>> ColumnsAsync(DatabaseFacade db, string schema, string table) =>
        db.SqlQueryRaw<string>(
            "SELECT COLUMN_NAME AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = {0} AND TABLE_NAME = {1}",
            schema, table).ToListAsync();

    private static Task<int> ScalarAsync(DatabaseFacade db, string sql) =>
        db.SqlQueryRaw<int>(sql).SingleAsync();
}
