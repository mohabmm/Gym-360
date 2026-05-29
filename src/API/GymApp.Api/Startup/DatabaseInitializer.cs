using GymApp.Api.Seed;
using GymApp.Modules.Billing.Infrastructure.Seed;
using GymApp.Modules.Identity.Infrastructure.Seed;
using GymApp.Modules.Nutrition.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Api.Startup;

/// <summary>
/// Applies EF migrations for every registered DbContext (and EnsureCreated() for
/// contexts without migrations, e.g. OutboxDbContext), then runs reference-data and
/// demo seeders. Tenancy-aware: per-gym DbContexts are migrated once per gym in
/// gyms_registry when running Hard; Soft mode runs each context once with no tenant set.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (!app.ShouldRunDbInit())
            return;

        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<Program>>();
        // Fallback: resolve each known context type explicitly since they're registered via AddDbContext<T>
        var dbContextTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("GymApp") == true)
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t))
            .ToList();

        // Per-gym context names (must match the per-gym DbContexts). Identity, OutboxDbContext,
        // and TenantRegistryDbContext are global and migrate exactly once.
        var perGymContextNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "GymsDbContext", "TrainingDbContext", "ProgressDbContext", "SchedulingDbContext",
            "NutritionDbContext", "CommunicationDbContext", "BillingDbContext",
            "GamificationDbContext", "OutboxDbContext"
        };

        var globalContexts = dbContextTypes.Where(t => !perGymContextNames.Contains(t.Name)).ToList();
        var perGymContexts = dbContextTypes.Where(t => perGymContextNames.Contains(t.Name)).ToList();

        async Task MigrateContextAsync(IServiceProvider innerSp, Type ctxType, string label)
        {
            try
            {
                var ctx = (DbContext?)innerSp.GetService(ctxType);
                if (ctx is null) return;
                var hasMigrations = ctx.Database.GetMigrations().Any();
                if (hasMigrations)
                {
                    await ctx.Database.MigrateAsync();
                    logger.LogInformation("Migrated {Context} [{Label}]", ctxType.Name, label);
                }
                else
                {
                    await ctx.Database.EnsureCreatedAsync();
                    logger.LogInformation("EnsureCreated {Context} [{Label}]", ctxType.Name, label);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize {Context} [{Label}]", ctxType.Name, label);
            }
        }

        // Step 1 — global contexts (Identity, TenantRegistry).
        foreach (var ctxType in globalContexts)
            await MigrateContextAsync(sp, ctxType, "global");

        // Step 1a — TenantRegistry uses EnsureCreated (no migrations), so column adds need
        // to be applied with an idempotent ALTER TABLE here. GymCode was added 2026-05-10
        // to back IGymRegistry.FindByCodeAsync without a per-gym SQL walk.
        try
        {
            var registry = sp.GetRequiredService<GymApp.Common.Infrastructure.Tenancy.Registry.TenantRegistryDbContext>();
            // SQL Server validates the entire batch up-front, so a literal CREATE INDEX
            // referencing GymCode would fail when the column doesn't yet exist. Use
            // sp_executesql so the ALTER and CREATE INDEX parse independently, after
            // the column physically exists.
            await registry.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('gyms_registry.gyms', 'GymCode') IS NULL
    EXEC sp_executesql N'ALTER TABLE [gyms_registry].[gyms] ADD [GymCode] NVARCHAR(6) NULL';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_gyms_GymCode' AND object_id = OBJECT_ID('gyms_registry.gyms'))
    EXEC sp_executesql N'CREATE UNIQUE INDEX [IX_gyms_GymCode] ON [gyms_registry].[gyms]([GymCode]) WHERE [GymCode] IS NOT NULL';");

            // One-time backfill: copy GymCode from each per-gym schema's gyms table.
            var rowsToBackfill = await registry.Gyms
                .Where(g => g.GymCode == null)
                .Select(g => new { g.GymId, g.SchemaPrefix })
                .ToListAsync();
            var safe = new System.Text.RegularExpressions.Regex(@"^g\d+$");
            foreach (var row in rowsToBackfill)
            {
                if (!safe.IsMatch(row.SchemaPrefix)) continue;
                try
                {
                    await registry.Database.ExecuteSqlRawAsync(
                        $"UPDATE [gyms_registry].[gyms] SET GymCode = (SELECT TOP 1 UPPER(GymCode) FROM [{row.SchemaPrefix}_gyms].gyms WHERE Id = {{0}}) WHERE GymId = {{0}} AND GymCode IS NULL",
                        row.GymId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "GymCode backfill skipped for gym {GymId} (prefix {Prefix})", row.GymId, row.SchemaPrefix);
                }
            }
            if (rowsToBackfill.Count > 0)
                logger.LogInformation("GymCode backfill attempted on {Count} legacy gyms_registry rows", rowsToBackfill.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tenant registry GymCode column patch failed");
        }

        var tenancy = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GymApp.Common.Infrastructure.Tenancy.TenancyOptions>>().Value;

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
        if (tenancy.Mode == GymApp.Common.Infrastructure.Tenancy.TenancyMode.Hard)
        {
            try
            {
                using var legacyScope = app.Services.CreateScope();
                // No accessor.Set() — leaves tenant unresolved so the model uses the
                // default `billing` schema instead of `g{N}_billing`.
                var legacyBilling = legacyScope.ServiceProvider.GetRequiredService<GymApp.Modules.Billing.Infrastructure.Data.BillingDbContext>();
                await legacyBilling.Database.MigrateAsync();
                await BillingSeeder.SeedAsync(legacyScope.ServiceProvider);
                logger.LogInformation("Hard-mode bootstrap: legacy billing.* reference data ensured.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Hard-mode legacy billing bootstrap failed; RegisterGym will likely 500 on first call.");
            }
        }

        // Step 2 — per-gym contexts.
        if (tenancy.Mode == GymApp.Common.Infrastructure.Tenancy.TenancyMode.Hard)
        {
            var registry = sp.GetRequiredService<GymApp.Common.Application.Tenancy.IGymRegistry>();
            var gyms = await registry.GetActiveAsync();
            foreach (var gym in gyms)
            {
                using var perGymScope = app.Services.CreateScope();
                var accessor = perGymScope.ServiceProvider.GetRequiredService<GymApp.Common.Application.Tenancy.ITenantContextAccessor>();
                accessor.Set(gym.GymId, gym.SchemaPrefix, isOwnerScope: false);
                foreach (var ctxType in perGymContexts)
                    await MigrateContextAsync(perGymScope.ServiceProvider, ctxType, $"gym {gym.SchemaPrefix}");
            }

            // Step 2b — Per-gym billing rehydration. Re-copies plans/features into
            // every g{N}_billing schema and ensures gym_subscriptions has an active
            // row pointing at the correct plan (derived from the owner's contract
            // PlanTier). Self-heals after a data wipe — without this, wiping
            // billing rows leaves IsFeatureEnabledAsync returning false for every
            // gated feature (e.g. session_packages).
            try
            {
                using var rehydrateScope = app.Services.CreateScope();
                var provisioning = rehydrateScope.ServiceProvider
                    .GetRequiredService<GymApp.Common.Infrastructure.Tenancy.Provisioning.IGymProvisioningService>();
                foreach (var gym in gyms)
                {
                    try
                    {
                        await provisioning.RehydrateBillingAsync(gym);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Per-gym billing rehydration failed for {Prefix}", gym.SchemaPrefix);
                    }
                }
                logger.LogInformation("Per-gym billing rehydration complete for {Count} gym(s)", gyms.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Per-gym billing rehydration outer scope failed");
            }
        }
        else
        {
            foreach (var ctxType in perGymContexts)
                await MigrateContextAsync(sp, ctxType, "soft");
        }

        // IdentityDevSeeder is global (writes to identity schema, no per-gym scope).
        // Gated by Identity:RunDevSeeder. Default false so production-like envs stay clean;
        // local dev opts in via appsettings.Development.json or env var.
        {
            var runIdentitySeeder = app.Configuration.GetValue<bool>("Identity:RunDevSeeder", false);
            if (runIdentitySeeder)
            {
                using var identityScope = app.Services.CreateScope();
                try { await IdentityDevSeeder.SeedAsync(identityScope.ServiceProvider); }
                catch (Exception ex) { logger.LogError(ex, "IdentityDevSeeder failed"); }
            }
            else
            {
                logger.LogInformation("IdentityDevSeeder disabled (Identity:RunDevSeeder=false).");
            }
        }

        // Per-gym seeders. In Hard mode each runs once per active gym inside the
        // tenant scope so EF resolves the correct g{N}_* schema. In Soft mode they
        // run once against the legacy module schemas (current default).
        //
        // NOTE: in Hard mode after bootstrap, demo data already exists in g1_*; the
        // per-gym dev seeders are skipped by default to avoid stomping on the
        // bootstrap-migrated state. Set Tenancy:RunDevSeedersOnHard=true to opt in
        // (useful when provisioning a brand-new gym from scratch in dev).
        async Task RunPerGymSeedersAsync()
        {
            // Demo data (FirstGym + owner@/staff@/trainer@/trainee@demo.local) only when
            // Seed:RunDemoData=true. Reference data (Billing plans, Nutrition food DB) always runs.
            var runDemoData = app.Configuration.GetValue<bool>("Seed:RunDemoData", false);

            if (tenancy.Mode == GymApp.Common.Infrastructure.Tenancy.TenancyMode.Hard)
            {
                var runDevSeeders = app.Configuration.GetValue<bool>("Tenancy:RunDevSeedersOnHard", false);
                if (!runDevSeeders)
                {
                    logger.LogInformation("Hard mode: skipping per-gym dev seeders (existing g{{N}}_* data preserved). Set Tenancy:RunDevSeedersOnHard=true to opt in.");
                    return;
                }

                var registry = sp.GetRequiredService<GymApp.Common.Application.Tenancy.IGymRegistry>();
                var gyms = await registry.GetActiveAsync();

                // Reference-data seeders (BillingSeeder, Nutrition food DB) are per-gym and run
                // for every tenant. DemoGymSeeder is bound to ONE specific tenant (the bootstrap
                // gym, conventionally g1) and must NOT run for every gym in the registry — that
                // would mint a "FirstGym/DEMO01" row in every tenant schema.
                foreach (var gym in gyms)
                {
                    using var seedScope = app.Services.CreateScope();
                    var accessor = seedScope.ServiceProvider.GetRequiredService<GymApp.Common.Application.Tenancy.ITenantContextAccessor>();
                    accessor.Set(gym.GymId, gym.SchemaPrefix, isOwnerScope: false);

                    try { await BillingSeeder.SeedAsync(seedScope.ServiceProvider); }
                    catch (Exception ex) { logger.LogError(ex, "BillingSeeder failed for gym {Prefix}", gym.SchemaPrefix); }

                    try { await NutritionModuleRegistration.SeedFoodDatabaseAsync(seedScope.ServiceProvider); }
                    catch (Exception ex) { logger.LogError(ex, "Nutrition food DB seed failed for gym {Prefix}", gym.SchemaPrefix); }
                }

                if (runDemoData)
                {
                    // Demo gym lives at g1 by convention (first gym ever provisioned).
                    // Pin the tenant context to g1 and run DemoGymSeeder once.
                    var demoGym = gyms.OrderBy(g => g.SequentialNumber).FirstOrDefault();
                    if (demoGym is null)
                    {
                        logger.LogWarning("DemoGymSeeder skipped: no gyms in registry yet.");
                    }
                    else
                    {
                        using var seedScope = app.Services.CreateScope();
                        var accessor = seedScope.ServiceProvider.GetRequiredService<GymApp.Common.Application.Tenancy.ITenantContextAccessor>();
                        accessor.Set(demoGym.GymId, demoGym.SchemaPrefix, isOwnerScope: false);
                        try { await DemoGymSeeder.SeedAsync(seedScope.ServiceProvider); }
                        catch (Exception ex) { logger.LogError(ex, "DemoGymSeeder failed for gym {Prefix}", demoGym.SchemaPrefix); }
                    }
                }
            }
            else
            {
                using var seedScope = app.Services.CreateScope();
                try { await BillingSeeder.SeedAsync(seedScope.ServiceProvider); }
                catch (Exception ex) { logger.LogError(ex, "BillingSeeder failed"); }

                try { await NutritionModuleRegistration.SeedFoodDatabaseAsync(seedScope.ServiceProvider); }
                catch (Exception ex) { logger.LogError(ex, "Nutrition food DB seed failed"); }

                if (runDemoData)
                {
                    try { await DemoGymSeeder.SeedAsync(seedScope.ServiceProvider); }
                    catch (Exception ex) { logger.LogError(ex, "DemoGymSeeder failed"); }
                }
                else
                {
                    logger.LogInformation("DemoGymSeeder disabled (Seed:RunDemoData=false).");
                }
            }
        }

        await RunPerGymSeedersAsync();
    }
}
