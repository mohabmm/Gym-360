using GymApp.Api;
using GymApp.Api.Hubs;
using GymApp.Api.Middleware;
using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Behaviors;
using GymApp.Common.Infrastructure.Authentication;
using GymApp.Common.Infrastructure.Caching;
using GymApp.Common.Infrastructure.Clock;
using GymApp.Common.Infrastructure.Interceptors;
using GymApp.Common.Infrastructure.Outbox;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Common.Presentation;
using GymApp.Api.Seed;
using GymApp.Modules.Identity.Infrastructure;
using GymApp.Modules.Identity.Infrastructure.Seed;
using GymApp.Modules.Billing.Infrastructure.Seed;
using GymApp.Modules.Gyms.Infrastructure;
using GymApp.Modules.Training.Infrastructure;
using GymApp.Modules.Progress.Infrastructure;
using GymApp.Modules.Scheduling.Infrastructure;
using GymApp.Modules.Nutrition.Infrastructure;
using GymApp.Modules.Communication.Infrastructure;
using GymApp.Modules.Billing.Infrastructure;
using GymApp.Modules.Gamification.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Serilog;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Ensure all module assemblies are loaded for Outbox type resolution ---
// The Newtonsoft.Json outbox processor uses TypeNameHandling.All and needs all domain event
// assemblies in the AppDomain before it can deserialize events from other modules.
_ = typeof(GymApp.Modules.Identity.Domain.Errors.UserErrors).Assembly;
_ = typeof(GymApp.Modules.Gyms.Domain.Errors.GymErrors).Assembly;
_ = typeof(GymApp.Modules.Training.Domain.Errors.TrainingErrors).Assembly;
_ = typeof(GymApp.Modules.Progress.Domain.ProgressErrors).Assembly;
_ = typeof(GymApp.Modules.Scheduling.Domain.Errors.SchedulingErrors).Assembly;
_ = typeof(GymApp.Modules.Nutrition.Domain.Errors.NutritionErrors).Assembly;
_ = typeof(GymApp.Modules.Communication.Domain.Errors.CommunicationErrors).Assembly;
_ = typeof(GymApp.Modules.Billing.Domain.Errors.BillingErrors).Assembly;
_ = typeof(GymApp.Modules.Gamification.Domain.Errors.GamificationErrors).Assembly;

// --- Force-load Presentation assemblies so the endpoint scan below can discover IEndpoint types.
// Project references alone don't load assemblies until the runtime needs them; the internal sealed
// Endpoint classes are never referenced by name from here, so we load the DLLs explicitly.
foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "GymApp.Modules.*.Presentation.dll"))
{
    try { System.Reflection.Assembly.LoadFrom(dll); } catch { /* ignore */ }
}

// --- Serilog ---
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// --- Authentication ---
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtOptions>(jwtSection);

// Fail-fast: HS256 needs ≥256 bits of entropy. Reject empty / placeholder / short keys
// at startup so misconfigured deployments never sign tokens with a weak secret.
{
    var jwtBoot = jwtSection.Get<JwtOptions>();
    var secret = jwtBoot?.SecretKey ?? string.Empty;
    if (string.IsNullOrWhiteSpace(secret)
        || secret.StartsWith("REPLACE_VIA_ENV", StringComparison.OrdinalIgnoreCase)
        || secret.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
        || Encoding.UTF8.GetByteCount(secret) < 32)
    {
        throw new InvalidOperationException(
            "Jwt:SecretKey is missing, placeholder, or shorter than 32 bytes. " +
            "Set JWT_SECRET_KEY env (generate via: openssl rand -base64 64).");
    }
    if (string.IsNullOrWhiteSpace(jwtBoot!.Issuer) || string.IsNullOrWhiteSpace(jwtBoot.Audience))
    {
        throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
    }
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = jwtSection.Get<JwtOptions>()!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        // SignalR JS client passes the JWT via ?access_token=... on the WebSocket
        // negotiate / connect request because browsers don't permit custom headers
        // on WS handshakes. Pull it into the principal so [Authorize] hubs work.
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// --- Authorization Policies (keyed off JWT claims emitted by JwtTokenService) ---
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("platform_admin", p => p.RequireAuthenticatedUser().RequireClaim("adminRole"))
    .AddPolicy("AdminPolicy", p => p.RequireAuthenticatedUser().RequireClaim("adminRole"))
    .AddPolicy("gym_owner", p => p.RequireAuthenticatedUser().RequireClaim("staffRole", "owner"))
    .AddPolicy("gym_staff", p => p.RequireAuthenticatedUser().RequireClaim("staffRole"))
    .AddPolicy("gym_owner_or_admin", p => p.RequireAuthenticatedUser().RequireAssertion(ctx =>
        ctx.User.HasClaim(c => c.Type == "adminRole") ||
        ctx.User.HasClaim(c => c.Type == "staffRole" && c.Value == "owner")))
    .AddPolicy("gym_owner_or_manager", p => p.RequireAuthenticatedUser().RequireAssertion(ctx =>
        // Owner principals (staffRole=owner) OR staff with gym-side sub-role of
        // Owner/Manager (carried as `staffSubRole` claim by IdentityJwtTokenService).
        ctx.User.HasClaim(c => c.Type == "staffRole" && c.Value == "owner") ||
        ctx.User.HasClaim(c =>
            c.Type == "staffSubRole" &&
            (string.Equals(c.Value, "Owner", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(c.Value, "Manager", StringComparison.OrdinalIgnoreCase)))))
    // JwtBearer remaps the JWT "role" claim to ClaimTypes.Role on the
    // ClaimsPrincipal (MapInboundClaims = true by default), so RequireClaim
    // against the raw "role" key never matches. RequireRole reads
    // ClaimTypes.Role under the hood, which is what we want.
    .AddPolicy("trainer", p => p.RequireAuthenticatedUser().RequireRole("trainer"))
    .AddPolicy("trainee", p => p.RequireAuthenticatedUser().RequireRole("trainee"));

// --- Common services ---
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<ConvertDomainEventsToOutboxMessagesInterceptor>();

// --- Tenancy ---
// Default mode = Soft (legacy schema names). Switch to Hard for per-Gym isolation.
builder.Services.AddTenancy(builder.Configuration);

// --- Redis ---
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// --- Outbox DbContext ---
builder.Services.AddDbContext<OutboxDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Database"))
        .UseTenantSchemaResolution());

builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));

// --- Quartz (Outbox processor) ---
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey(nameof(ProcessOutboxMessagesJob));
    q.AddJob<ProcessOutboxMessagesJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity($"{nameof(ProcessOutboxMessagesJob)}-trigger")
        .WithSimpleSchedule(schedule => schedule
            .WithIntervalInSeconds(10)
            .RepeatForever()));
});
builder.Services.AddQuartzHostedService();

// --- MediatR (API assembly only — each module registers its own handlers via Add{Module}Module) ---
// Pipeline behaviors run for every request across every module.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// --- Module Registration (each module adds its own DbContext + services) ---
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddGymsModule(builder.Configuration);
builder.Services.AddTrainingModule(builder.Configuration);
builder.Services.AddProgressModule(builder.Configuration);
builder.Services.AddSchedulingModule(builder.Configuration);
builder.Services.AddNutritionModule(builder.Configuration);
builder.Services.AddCommunicationModule(builder.Configuration);
builder.Services.AddBillingModule(builder.Configuration);
builder.Services.AddGamificationModule(builder.Configuration);

// --- Exception handler ---
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// --- JSON serialization: camelCase properties, case-insensitive enum parsing ---
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
});

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "GymApp API", Version = "v1" });
    // Many endpoints declare nested "Request" records (e.g. AddMemberEndpoint.Request).
    // The default schemaId selector uses the short name and collides across modules.
    options.CustomSchemaIds(t => t.FullName!.Replace('+', '.'));
    // Emit enums as their camelCase string names to match the JsonStringEnumConverter
    // registered on ConfigureHttpJsonOptions — otherwise clients see "enum":[0,1,2]
    // while the server expects "flagOnly"/"notifyTrainer"/"suspendBooking".
    options.UseAllOfToExtendReferenceSchemas();
    options.SchemaFilter<StringEnumSchemaFilter>();
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// --- SignalR ---
builder.Services.AddSignalR();
builder.Services.AddScoped<GymApp.Common.Application.Abstractions.IChatNotifier, GymApp.Api.Hubs.SignalRChatNotifier>();

// --- Health Checks ---
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OutboxDbContext>("outbox-db", tags: new[] { "ready" })
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
        name: "redis",
        tags: new[] { "ready" });

// --- CORS ---
// Origins are read from Cors:AllowedOrigins. We NEVER fall back to AllowAnyOrigin:
// if the configured list is empty we warn and use a localhost-only fallback so
// misconfiguration fails closed rather than open.
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

string[] allowedOrigins;
if (configuredOrigins.Length == 0)
{
    allowedOrigins = new[] { "http://localhost:3000", "http://localhost:3001" };
    Console.Error.WriteLine(
        $"[WARN] CORS:AllowedOrigins is empty. Falling back to localhost-only origins: " +
        $"{string.Join(", ", allowedOrigins)}. Configure Cors:AllowedOrigins in appsettings " +
        $"or the CORS_ORIGIN_* env vars for production.");
}
else
{
    allowedOrigins = configuredOrigins;
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // needed for SignalR WebSocket
    });
});

var app = builder.Build();

// --- Hard-tenancy bootstrap + version gate ------------------------------------
// Soft mode skips entirely. In Hard mode the platform refuses to start until
// dbo.__tenancy_version >= 1. On a *fresh* database we self-bootstrap that
// marker (mirrors scripts/phase0_reset.sh) so production/staging deploys come
// up without a manual SQL step. If legacy *soft* data is present we do NOT
// auto-stamp — that path requires the maintenance-window migration
// src/DB/migrations/2026_05_01_hard_isolation_bootstrap.sql, and auto-stamping
// would silently corrupt tenancy.
var runDbInit = app.Environment.IsDevelopment()
    || builder.Configuration.GetValue<bool>("Database:AutoMigrate", false);
{
    var tenancyMode = builder.Configuration.GetSection("Tenancy:Mode").Value ?? "Soft";
    if (string.Equals(tenancyMode, "Hard", StringComparison.OrdinalIgnoreCase))
    {
        var connStr = builder.Configuration.GetConnectionString("Database");
        try
        {
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(connStr);
            // Bounded retry: the gate runs before EF (which has its own resiliency),
            // so a cold-start SQL race must NOT silently skip the fresh-DB bootstrap
            // — otherwise __tenancy_version is never stamped and the next restart
            // crash-loops on the version gate.
            for (var attempt = 1; ; attempt++)
            {
                try { await conn.OpenAsync(); break; }
                catch (Microsoft.Data.SqlClient.SqlException) when (attempt < 10)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }

            async Task<int> ReadTenancyVersionAsync()
            {
                using var c = conn.CreateCommand();
                c.CommandText = "IF OBJECT_ID('dbo.__tenancy_version','U') IS NULL SELECT 0 ELSE SELECT ISNULL(MAX([Version]),0) FROM dbo.__tenancy_version";
                return (int)(await c.ExecuteScalarAsync() ?? 0);
            }

            var version = await ReadTenancyVersionAsync();
            if (version < 1)
            {
                // Detect pre-existing soft-tenancy data: any *unprefixed* per-module
                // business schema still carrying base tables. Deliberately excludes:
                //   - identity  : global cross-tenant in BOTH modes (a seeded admin
                //                  is NOT soft data — counting it crash-loops a fresh
                //                  Hard deploy whose bootstrap raced the DB cold start).
                //   - billing   : Step 1b intentionally keeps a legacy billing.* schema
                //                  in Hard mode as the hydration source for g{N}_billing.
                using var probe = conn.CreateCommand();
                probe.CommandText = @"
DECLARE @soft BIT = 0;
IF EXISTS (
    SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name IN ('gyms','training','progress','scheduling','nutrition','communication','gamification')
) SET @soft = 1;
SELECT @soft;";
                var hasSoftData = Convert.ToInt32(await probe.ExecuteScalarAsync() ?? 0) == 1;

                if (hasSoftData)
                    throw new InvalidOperationException(
                        "Tenancy:Mode=Hard but dbo.__tenancy_version < 1 and legacy soft-tenancy data is present. " +
                        "Run src/DB/migrations/2026_05_01_hard_isolation_bootstrap.sql in a maintenance window first.");

                if (!runDbInit)
                    throw new InvalidOperationException(
                        "Tenancy:Mode=Hard but dbo.__tenancy_version < 1 on a fresh database, and DB auto-init is off. " +
                        "Set Database:AutoMigrate=true (env Database__AutoMigrate=true) to self-bootstrap, " +
                        "or run src/DB/migrations/2026_05_01_hard_isolation_bootstrap.sql.");

                // Fresh DB + auto-init enabled → idempotent mini-bootstrap.
                using var boot = conn.CreateCommand();
                boot.CommandText = @"
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
                await boot.ExecuteNonQueryAsync();

                version = await ReadTenancyVersionAsync();
                if (version < 1)
                    throw new InvalidOperationException(
                        "Hard-mode self-bootstrap ran but dbo.__tenancy_version is still < 1. Aborting startup.");
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException) { /* DB not reachable yet — let normal startup error path handle it */ }
    }
}

// --- DB initialization: applies EF migrations for all registered DbContexts,
// and EnsureCreated() for contexts without migrations (e.g. OutboxDbContext).
// Runs in Development, or anywhere Database:AutoMigrate=true (env
// Database__AutoMigrate=true) — required for self-bootstrapping Dokploy deploys.
//
// Tenancy-aware: per-gym DbContexts are migrated once per gym in gyms_registry
// when running Hard. Soft mode just runs each context once with no tenant set.
if (runDbInit)
{
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
            await GymApp.Modules.Billing.Infrastructure.Seed.BillingSeeder.SeedAsync(legacyScope.ServiceProvider);
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
        var runIdentitySeeder = builder.Configuration.GetValue<bool>("Identity:RunDevSeeder", false);
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
        var runDemoData = builder.Configuration.GetValue<bool>("Seed:RunDemoData", false);

        if (tenancy.Mode == GymApp.Common.Infrastructure.Tenancy.TenancyMode.Hard)
        {
            var runDevSeeders = builder.Configuration.GetValue<bool>("Tenancy:RunDevSeedersOnHard", false);
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

// --- Pipeline ---
// ForwardedHeaders FIRST: behind Dokploy/Traefik (or any reverse proxy), trust
// X-Forwarded-Proto / X-Forwarded-For so Request.Scheme, Url.Action redirects,
// and client-IP logging reflect the real client. Must run before auth/CORS.
{
    var fwd = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
            | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost,
    };
    fwd.KnownNetworks.Clear();
    fwd.KnownProxies.Clear();
    app.UseForwardedHeaders(fwd);
}

// UseCors must run before UseExceptionHandler so error responses keep CORS headers;
// otherwise the browser surfaces a misleading "blocked by CORS" error masking the real 5xx.
app.UseCors();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseAuthentication();
app.UseAuthorization();

// --- Tenant resolution (no-op when Tenancy:Mode=Soft) ---
app.UseMiddleware<TenantResolutionMiddleware>();

// --- Health check endpoints ---
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// --- SignalR hubs ---
app.MapHub<ConfigHub>(ConfigHub.HubPath);
app.MapHub<GymApp.Api.Hubs.ChatHub>(GymApp.Api.Hubs.ChatHub.HubPath);

// --- Map module endpoints ---
var endpointTypes = AppDomain.CurrentDomain.GetAssemblies()
    .Where(a => a.FullName?.StartsWith("GymApp") == true)
    .SelectMany(a => a.GetTypes())
    .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

foreach (var endpointType in endpointTypes)
{
    var endpoint = (IEndpoint)Activator.CreateInstance(endpointType)!;
    endpoint.MapEndpoint(app);
}

app.Run();

public partial class Program { }
