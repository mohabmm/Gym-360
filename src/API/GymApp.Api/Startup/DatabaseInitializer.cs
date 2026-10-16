using GymApp.Api.Seed;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Common.Infrastructure.Tenancy.Provisioning;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using GymApp.Modules.Billing.Infrastructure.Data;
using GymApp.Modules.Billing.Infrastructure.Seed;
using GymApp.Modules.Identity.Infrastructure.Seed;
using GymApp.Modules.Nutrition.Infrastructure;
using GymApp.Modules.Training.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GymApp.Api.Startup;

/// <summary>
/// Applies EF migrations for every registered DbContext (and EnsureCreated() for
/// contexts without migrations, e.g. OutboxDbContext), then runs reference-data and
/// demo seeders. Tenancy-aware: per-gym DbContexts are migrated once per gym in
/// gyms_registry when running Hard; Soft mode runs each context once with no tenant set.
/// </summary>
public static class DatabaseInitializer
{
    // Per-gym context names (must match the per-gym DbContexts). Identity, OutboxDbContext,
    // and TenantRegistryDbContext are global and migrate exactly once.
    private static readonly HashSet<string> PerGymContextNames = new(StringComparer.Ordinal)
    {
        "GymsDbContext", "TrainingDbContext", "ProgressDbContext", "SchedulingDbContext",
        "NutritionDbContext", "CommunicationDbContext", "BillingDbContext",
        "GamificationDbContext", "OutboxDbContext"
    };

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (!app.ShouldRunDbInit())
            return;

        using var scope = app.Services.CreateScope();
        await new Runner(app, scope.ServiceProvider).RunAsync();
    }

    private sealed class Runner
    {
        private readonly WebApplication _app;
        private readonly IServiceProvider _sp;
        private readonly ILogger _logger;

        public Runner(WebApplication app, IServiceProvider sp)
        {
            _app = app;
            _sp = sp;
            _logger = sp.GetRequiredService<ILogger<Program>>();
        }

        private bool IsHardMode =>
            _sp.GetRequiredService<IOptions<TenancyOptions>>().Value.Mode == TenancyMode.Hard;

        public async Task RunAsync()
        {
            var dbContextTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName?.StartsWith("GymApp") == true)
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t))
                .ToList();
            var globalContexts = dbContextTypes.Where(t => !PerGymContextNames.Contains(t.Name)).ToList();
            var perGymContexts = dbContextTypes.Where(t => PerGymContextNames.Contains(t.Name)).ToList();

            // Step 1 — global contexts (Identity, TenantRegistry).
            foreach (var ctxType in globalContexts)
                await MigrateContextAsync(_sp, ctxType, "global");

            await PatchRegistryGymCodeAsync();
            await BackfillDurableOutboxAsync();

            var hardMode = IsHardMode;
            if (hardMode)
            {
                await BootstrapLegacyBillingAsync();
                await MigratePerGymContextsAsync(perGymContexts);
            }
            else
            {
                foreach (var ctxType in perGymContexts)
                    await MigrateContextAsync(_sp, ctxType, "soft");
            }

            await RunIdentityDevSeederAsync();

            if (hardMode)
                await RunHardModeSeedersAsync();
            else
                await RunSoftModeSeedersAsync();

            await BackfillExerciseNamesForAllAsync(hardMode);
        }

        private async Task MigrateContextAsync(IServiceProvider innerSp, Type ctxType, string label)
        {
            try
            {
                var ctx = (DbContext?)innerSp.GetService(ctxType);
                if (ctx is null) return;
                if (ctx.Database.GetMigrations().Any())
                {
                    await ctx.Database.MigrateAsync();
                    _logger.LogInformation("Migrated {Context} [{Label}]", ctxType.Name, label);
                }
                else
                {
                    await ctx.Database.EnsureCreatedAsync();
                    _logger.LogInformation("EnsureCreated {Context} [{Label}]", ctxType.Name, label);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize {Context} [{Label}]", ctxType.Name, label);
            }
        }

        /// <summary>Creates a DI scope with the tenant context pinned to <paramref name="gym"/>.</summary>
        private IServiceScope CreateGymScope(GymRegistration gym)
        {
            var gymScope = _app.Services.CreateScope();
            gymScope.ServiceProvider
                .GetRequiredService<ITenantContextAccessor>()
                .Set(gym.GymId, gym.SchemaPrefix, isOwnerScope: false);
            return gymScope;
        }

        private Task<IReadOnlyList<GymRegistration>> GetActiveGymsAsync() =>
            _sp.GetRequiredService<IGymRegistry>().GetActiveAsync();

        // Step 1a — TenantRegistry uses EnsureCreated (no migrations), so column adds need
        // to be applied with an idempotent ALTER TABLE here. GymCode was added 2026-05-10
        // to back IGymRegistry.FindByCodeAsync without a per-gym SQL walk.
        private async Task PatchRegistryGymCodeAsync()
        {
            try
            {
                var registry = _sp.GetRequiredService<TenantRegistryDbContext>();
                // SQL Server validates the entire batch up-front, so a literal CREATE INDEX
                // referencing GymCode would fail when the column doesn't yet exist. Use
                // sp_executesql so the ALTER and CREATE INDEX parse independently, after
                // the column physically exists.
                await registry.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('gyms_registry.gyms', 'GymCode') IS NULL
    EXEC sp_executesql N'ALTER TABLE [gyms_registry].[gyms] ADD [GymCode] NVARCHAR(6) NULL';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_gyms_GymCode' AND object_id = OBJECT_ID('gyms_registry.gyms'))
    EXEC sp_executesql N'CREATE UNIQUE INDEX [IX_gyms_GymCode] ON [gyms_registry].[gyms]([GymCode]) WHERE [GymCode] IS NOT NULL';");

                await BackfillGymCodesAsync(registry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant registry GymCode column patch failed");
            }
        }

        // One-time backfill: copy GymCode from each per-gym schema's gyms table.
        private async Task BackfillGymCodesAsync(TenantRegistryDbContext registry)
        {
            var rowsToBackfill = await registry.Gyms
                .Where(g => g.GymCode == null)
                .Select(g => new { g.GymId, g.SchemaPrefix })
                .ToListAsync();
            var safe = new System.Text.RegularExpressions.Regex(@"^g\d+$");
            foreach (var row in rowsToBackfill.Where(r => safe.IsMatch(r.SchemaPrefix)))
            {
                try
                {
                    await registry.Database.ExecuteSqlRawAsync(
                        $"UPDATE [gyms_registry].[gyms] SET GymCode = (SELECT TOP 1 UPPER(GymCode) FROM [{row.SchemaPrefix}_gyms].gyms WHERE Id = {{0}}) WHERE GymId = {{0}} AND GymCode IS NULL",
                        row.GymId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GymCode backfill skipped for gym {GymId} (prefix {Prefix})", row.GymId, row.SchemaPrefix);
                }
            }
            if (rowsToBackfill.Count > 0)
                _logger.LogInformation("GymCode backfill attempted on {Count} legacy gyms_registry rows", rowsToBackfill.Count);
        }

        // Step 1a' — Durable-outbox backfill (seed §0.6.1). OutboxDbContext has no
        // migrations (it uses EnsureCreated), so per-gym g{N}_outbox_messages tables
        // created before the durable-integration-events upgrade are NEVER altered by a
        // redeploy — EnsureCreated only CREATEs missing tables, it doesn't add columns.
        // Result: per-gym module WRITES (add-member enroll, announcement, …) 500 on the
        // outbox INSERT because RetryCount / the consumer ledger are missing, while reads
        // look healthy. This schema-agnostic, idempotent backfill adds them to EVERY
        // existing outbox_messages schema (excluding migration-owned 'identity'), so a
        // normal deploy self-heals. New gyms already get these from the EF model via
        // GymProvisioningService.CreateTablesAsync.
        private async Task BackfillDurableOutboxAsync()
        {
            try
            {
                var registry = _sp.GetRequiredService<TenantRegistryDbContext>();
                await registry.Database.ExecuteSqlRawAsync(@"
SET QUOTED_IDENTIFIER ON;
DECLARE @outboxUpgrade nvarchar(max) = N'';
SELECT @outboxUpgrade = @outboxUpgrade + N'
IF COL_LENGTH(' + QUOTENAME(s.name + N'.outbox_messages', '''') + N', ''RetryCount'') IS NULL
    ALTER TABLE ' + QUOTENAME(s.name) + N'.outbox_messages
        ADD RetryCount int NOT NULL CONSTRAINT ' + QUOTENAME(N'DF_' + s.name + N'_outbox_RetryCount') + N' DEFAULT 0;
IF OBJECT_ID(' + QUOTENAME(s.name + N'.outbox_message_consumers', '''') + N', ''U'') IS NULL
    CREATE TABLE ' + QUOTENAME(s.name) + N'.outbox_message_consumers (
        OutboxMessageId uniqueidentifier NOT NULL,
        Name            nvarchar(200)    NOT NULL,
        CONSTRAINT ' + QUOTENAME(N'PK_' + s.name + N'_outbox_message_consumers') + N' PRIMARY KEY (OutboxMessageId, Name)
    );'
FROM sys.schemas s
WHERE OBJECT_ID(s.name + N'.outbox_messages', 'U') IS NOT NULL
  AND s.name <> N'identity';
IF LEN(@outboxUpgrade) > 0 EXEC sp_executesql @outboxUpgrade;");
                _logger.LogInformation("Durable-outbox backfill (RetryCount + consumer ledger) ensured across existing schemas.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Durable-outbox backfill failed; per-gym writes may 500 on the outbox INSERT.");
            }
        }

        // Step 1b — Hard-mode reference data bootstrap.
        //
        // GymProvisioningService.HydrateBillingAsync copies plans/features into the
        // new gym's `g{N}_billing` from the legacy `billing.*` schema. On a fresh
        // database that legacy schema does not exist, so the very first RegisterGym
        // call returns 500 (`Invalid object name 'billing.subscription_plans'`).
        //
        // To make Hard-mode deploys self-bootstrapping, we resolve BillingDbContext
        // in a scope with no tenant context set: SchemaFor("billing") falls through
        // to the default `billing` and EF migrates + seeds the legacy tables once.
        // Idempotent on subsequent boots.
        private async Task BootstrapLegacyBillingAsync()
        {
            try
            {
                using var legacyScope = _app.Services.CreateScope();
                // No accessor.Set() — leaves tenant unresolved so the model uses the
                // default `billing` schema instead of `g{N}_billing`.
                var legacyBilling = legacyScope.ServiceProvider.GetRequiredService<BillingDbContext>();
                await legacyBilling.Database.MigrateAsync();
                await BillingSeeder.SeedAsync(legacyScope.ServiceProvider);
                _logger.LogInformation("Hard-mode bootstrap: legacy billing.* reference data ensured.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hard-mode legacy billing bootstrap failed; RegisterGym will likely 500 on first call.");
            }
        }

        // Step 2 — per-gym contexts (Hard mode).
        private async Task MigratePerGymContextsAsync(List<Type> perGymContexts)
        {
            var gyms = await GetActiveGymsAsync();
            foreach (var gym in gyms)
            {
                using var perGymScope = CreateGymScope(gym);
                foreach (var ctxType in perGymContexts)
                    await MigrateContextAsync(perGymScope.ServiceProvider, ctxType, $"gym {gym.SchemaPrefix}");
            }

            await RehydrateBillingAsync(gyms);
        }

        // Step 2b — Per-gym billing rehydration. Re-copies plans/features into
        // every g{N}_billing schema and ensures gym_subscriptions has an active
        // row pointing at the correct plan (derived from the owner's contract
        // PlanTier). Self-heals after a data wipe — without this, wiping
        // billing rows leaves IsFeatureEnabledAsync returning false for every
        // gated feature (e.g. session_packages).
        private async Task RehydrateBillingAsync(IReadOnlyList<GymRegistration> gyms)
        {
            try
            {
                using var rehydrateScope = _app.Services.CreateScope();
                var provisioning = rehydrateScope.ServiceProvider.GetRequiredService<IGymProvisioningService>();
                foreach (var gym in gyms)
                {
                    try
                    {
                        await provisioning.RehydrateBillingAsync(gym);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Per-gym billing rehydration failed for {Prefix}", gym.SchemaPrefix);
                    }
                }
                _logger.LogInformation("Per-gym billing rehydration complete for {Count} gym(s)", gyms.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Per-gym billing rehydration outer scope failed");
            }
        }

        // IdentityDevSeeder is global (writes to identity schema, no per-gym scope).
        // Gated by Identity:RunDevSeeder. Default false so production-like envs stay clean;
        // local dev opts in via appsettings.Development.json or env var.
        private async Task RunIdentityDevSeederAsync()
        {
            if (!_app.Configuration.GetValue<bool>("Identity:RunDevSeeder", false))
            {
                _logger.LogInformation("IdentityDevSeeder disabled (Identity:RunDevSeeder=false).");
                return;
            }

            using var identityScope = _app.Services.CreateScope();
            try { await IdentityDevSeeder.SeedAsync(identityScope.ServiceProvider); }
            catch (Exception ex) { _logger.LogError(ex, "IdentityDevSeeder failed"); }
        }

        // Demo data (FirstGym + owner@/staff@/trainer@/trainee@demo.local) only when
        // Seed:RunDemoData=true. Reference data (Billing plans, Nutrition food DB) always runs.
        private bool RunDemoData => _app.Configuration.GetValue<bool>("Seed:RunDemoData", false);

        // Per-gym seeders, Hard mode: each runs once per active gym inside the tenant
        // scope so EF resolves the correct g{N}_* schema.
        //
        // NOTE: in Hard mode after bootstrap, demo data already exists in g1_*; the
        // per-gym dev seeders are skipped by default to avoid stomping on the
        // bootstrap-migrated state. Set Tenancy:RunDevSeedersOnHard=true to opt in
        // (useful when provisioning a brand-new gym from scratch in dev).
        private async Task RunHardModeSeedersAsync()
        {
            if (!_app.Configuration.GetValue<bool>("Tenancy:RunDevSeedersOnHard", false))
            {
                _logger.LogInformation("Hard mode: skipping per-gym dev seeders (existing g{{N}}_* data preserved). Set Tenancy:RunDevSeedersOnHard=true to opt in.");
                return;
            }

            var gyms = await GetActiveGymsAsync();

            // Reference-data seeders (BillingSeeder, Nutrition food DB) are per-gym and run
            // for every tenant. DemoGymSeeder is bound to ONE specific tenant (the bootstrap
            // gym, conventionally g1) and must NOT run for every gym in the registry — that
            // would mint a "FirstGym/DEMO01" row in every tenant schema.
            foreach (var gym in gyms)
            {
                using var seedScope = CreateGymScope(gym);
                await SeedReferenceDataAsync(seedScope.ServiceProvider, $" for gym {gym.SchemaPrefix}");
            }

            if (RunDemoData)
                await SeedDemoGymAsync(gyms);
        }

        // Demo gym lives at g1 by convention (first gym ever provisioned).
        // Pin the tenant context to g1 and run DemoGymSeeder once.
        private async Task SeedDemoGymAsync(IReadOnlyList<GymRegistration> gyms)
        {
            var demoGym = gyms.OrderBy(g => g.SequentialNumber).FirstOrDefault();
            if (demoGym is null)
            {
                _logger.LogWarning("DemoGymSeeder skipped: no gyms in registry yet.");
                return;
            }

            using var seedScope = CreateGymScope(demoGym);
            try { await DemoGymSeeder.SeedAsync(seedScope.ServiceProvider); }
            catch (Exception ex) { _logger.LogError(ex, "DemoGymSeeder failed for gym {Prefix}", demoGym.SchemaPrefix); }
        }

        // Per-gym seeders, Soft mode: run once against the legacy module schemas.
        private async Task RunSoftModeSeedersAsync()
        {
            using var seedScope = _app.Services.CreateScope();
            await SeedReferenceDataAsync(seedScope.ServiceProvider, string.Empty);

            if (!RunDemoData)
            {
                _logger.LogInformation("DemoGymSeeder disabled (Seed:RunDemoData=false).");
                return;
            }

            try { await DemoGymSeeder.SeedAsync(seedScope.ServiceProvider); }
            catch (Exception ex) { _logger.LogError(ex, "DemoGymSeeder failed"); }
        }

        private async Task SeedReferenceDataAsync(IServiceProvider scoped, string suffix)
        {
            try { await BillingSeeder.SeedAsync(scoped); }
            catch (Exception ex) { _logger.LogError(ex, "BillingSeeder failed{Suffix}", suffix); }

            try { await NutritionModuleRegistration.SeedFoodDatabaseAsync(scoped); }
            catch (Exception ex) { _logger.LogError(ex, "Nutrition food DB seed failed{Suffix}", suffix); }
        }

        // Arabic names for the global exercise library. Unlike the dev seeders above this
        // is a data fix every tenant needs (the library shipped English-only), so it runs
        // for all gyms in Hard mode too. Idempotent and non-fatal.
        private async Task BackfillExerciseNamesForAllAsync(bool hardMode)
        {
            if (!hardMode)
            {
                using var softScope = _app.Services.CreateScope();
                await BackfillExerciseNamesAsync(softScope.ServiceProvider, "soft");
                return;
            }

            foreach (var gym in await GetActiveGymsAsync())
            {
                using var gymScope = CreateGymScope(gym);
                await BackfillExerciseNamesAsync(gymScope.ServiceProvider, $"gym {gym.SchemaPrefix}");
            }
        }

        private async Task BackfillExerciseNamesAsync(IServiceProvider scoped, string label)
        {
            try
            {
                var updated = await TrainingModuleRegistration.BackfillExerciseArabicNamesAsync(scoped);
                if (updated > 0)
                    _logger.LogInformation("Backfilled {Count} Arabic exercise names [{Label}]", updated, label);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exercise Arabic-name backfill failed [{Label}]", label);
            }
        }
    }
}
