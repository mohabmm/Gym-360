namespace GymApp.Api.Startup;

/// <summary>
/// Hard-tenancy version gate + fresh-database self-bootstrap. No-op in Soft mode.
/// </summary>
public static class HardTenancyBootstrap
{
    /// <summary>
    /// DB init runs in Development, or anywhere Database:AutoMigrate=true
    /// (env Database__AutoMigrate=true) — required for self-bootstrapping Dokploy deploys.
    /// </summary>
    internal static bool ShouldRunDbInit(this WebApplication app) =>
        app.Environment.IsDevelopment()
        || app.Configuration.GetValue<bool>("Database:AutoMigrate", false);

    // --- Hard-tenancy bootstrap + version gate ------------------------------------
    // Soft mode skips entirely. In Hard mode the platform refuses to start until
    // dbo.__tenancy_version >= 1. On a *fresh* database we self-bootstrap that
    // marker (mirrors scripts/phase0_reset.sh) so production/staging deploys come
    // up without a manual SQL step. If legacy *soft* data is present we do NOT
    // auto-stamp — that path requires the maintenance-window migration
    // src/DB/migrations/2026_05_01_hard_isolation_bootstrap.sql, and auto-stamping
    // would silently corrupt tenancy.
    public static async Task RunHardTenancyBootstrapAsync(this WebApplication app)
    {
        var runDbInit = app.ShouldRunDbInit();

        var tenancyMode = app.Configuration.GetSection("Tenancy:Mode").Value ?? "Soft";
        if (!string.Equals(tenancyMode, "Hard", StringComparison.OrdinalIgnoreCase))
            return;

        var connStr = app.Configuration.GetConnectionString("Database");
        try
        {
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(connStr);
            await OpenWithRetryAsync(conn);

            if (await ReadTenancyVersionAsync(conn) < 1)
                await BootstrapFreshDatabaseAsync(conn, runDbInit);
        }
        catch (Microsoft.Data.SqlClient.SqlException) { /* DB not reachable yet — let normal startup error path handle it */ }
    }

    private const string ReadTenancyVersionSql =
        "IF OBJECT_ID('dbo.__tenancy_version','U') IS NULL SELECT 0 ELSE SELECT ISNULL(MAX([Version]),0) FROM dbo.__tenancy_version";

    // Detect pre-existing soft-tenancy data: any *unprefixed* per-module
    // business schema still carrying base tables. Deliberately excludes:
    //   - identity  : global cross-tenant in BOTH modes (a seeded admin
    //                  is NOT soft data — counting it crash-loops a fresh
    //                  Hard deploy whose bootstrap raced the DB cold start).
    //   - billing   : Step 1b intentionally keeps a legacy billing.* schema
    //                  in Hard mode as the hydration source for g{N}_billing.
    private const string SoftDataProbeSql = @"
DECLARE @soft BIT = 0;
IF EXISTS (
    SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name IN ('gyms','training','progress','scheduling','nutrition','communication','gamification')
) SET @soft = 1;
SELECT @soft;";

    // Fresh DB + auto-init enabled → idempotent mini-bootstrap.
    private const string FreshBootstrapSql = @"
IF OBJECT_ID('dbo.__tenancy_version','U') IS NULL
    CREATE TABLE dbo.__tenancy_version ([Version] INT NOT NULL PRIMARY KEY, AppliedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), Note NVARCHAR(500) NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.__tenancy_version WHERE [Version] = 1)
    INSERT INTO dbo.__tenancy_version ([Version], Note) VALUES (1, N'fresh-DB self-bootstrap (Program.cs startup)');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'gyms_registry') EXEC('CREATE SCHEMA [gyms_registry]');
IF OBJECT_ID('gyms_registry.gyms','U') IS NULL
BEGIN
    CREATE TABLE gyms_registry.gyms (
        GymId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        OwnerId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Slug NVARCHAR(100) NOT NULL,
        SchemaPrefix NVARCHAR(20) NOT NULL,
        SequentialNumber INT IDENTITY(1,1) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        CONSTRAINT UQ_gyms_registry_gyms_SchemaPrefix UNIQUE (SchemaPrefix),
        CONSTRAINT UQ_gyms_registry_gyms_Slug UNIQUE (Slug),
        CONSTRAINT UQ_gyms_registry_gyms_SequentialNumber UNIQUE (SequentialNumber)
    );
    CREATE INDEX IX_gyms_registry_gyms_OwnerId ON gyms_registry.gyms (OwnerId);
END;";

    // Bounded retry: the gate runs before EF (which has its own resiliency),
    // so a cold-start SQL race must NOT silently skip the fresh-DB bootstrap
    // — otherwise __tenancy_version is never stamped and the next restart
    // crash-loops on the version gate.
    private static async Task OpenWithRetryAsync(Microsoft.Data.SqlClient.SqlConnection conn)
    {
        for (var attempt = 1; ; attempt++)
        {
            try { await conn.OpenAsync(); break; }
            catch (Microsoft.Data.SqlClient.SqlException) when (attempt < 10)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }

    private static async Task<int> ReadTenancyVersionAsync(Microsoft.Data.SqlClient.SqlConnection conn)
    {
        using var c = conn.CreateCommand();
        c.CommandText = ReadTenancyVersionSql;
        return (int)(await c.ExecuteScalarAsync() ?? 0);
    }

    private static async Task<bool> HasSoftTenancyDataAsync(Microsoft.Data.SqlClient.SqlConnection conn)
    {
        using var probe = conn.CreateCommand();
        probe.CommandText = SoftDataProbeSql;
        return Convert.ToInt32(await probe.ExecuteScalarAsync() ?? 0) == 1;
    }

    private static async Task BootstrapFreshDatabaseAsync(Microsoft.Data.SqlClient.SqlConnection conn, bool runDbInit)
    {
        if (await HasSoftTenancyDataAsync(conn))
            throw new InvalidOperationException(
                "Tenancy:Mode=Hard but dbo.__tenancy_version < 1 and legacy soft-tenancy data is present. " +
                "Run src/DB/migrations/2026_05_01_hard_isolation_bootstrap.sql in a maintenance window first.");

        if (!runDbInit)
            throw new InvalidOperationException(
                "Tenancy:Mode=Hard but dbo.__tenancy_version < 1 on a fresh database, and DB auto-init is off. " +
                "Set Database:AutoMigrate=true (env Database__AutoMigrate=true) to self-bootstrap, " +
                "or run src/DB/migrations/2026_05_01_hard_isolation_bootstrap.sql.");

        using var boot = conn.CreateCommand();
        boot.CommandText = FreshBootstrapSql;
        await boot.ExecuteNonQueryAsync();

        if (await ReadTenancyVersionAsync(conn) < 1)
            throw new InvalidOperationException(
                "Hard-mode self-bootstrap ran but dbo.__tenancy_version is still < 1. Aborting startup.");
    }
}
