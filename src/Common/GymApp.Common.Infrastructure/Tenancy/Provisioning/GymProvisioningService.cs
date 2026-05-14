using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymApp.Common.Infrastructure.Tenancy.Provisioning;

internal sealed class GymProvisioningService : IGymProvisioningService, IGymProvisioner
{
    private readonly TenantRegistryDbContext _registry;
    private readonly SchemaCreator _schemaCreator;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GymProvisioningService> _logger;

    public GymProvisioningService(
        TenantRegistryDbContext registry,
        SchemaCreator schemaCreator,
        IServiceScopeFactory scopeFactory,
        ILogger<GymProvisioningService> logger)
    {
        _registry = registry;
        _schemaCreator = schemaCreator;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<GymRegistration> ProvisionAsync(
        Guid gymId,
        Guid ownerId,
        string name,
        string slug,
        string? gymCode = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = string.IsNullOrWhiteSpace(gymCode) ? null : gymCode.Trim().ToUpperInvariant();

        var existing = await _registry.Gyms.FirstOrDefaultAsync(g => g.GymId == gymId, cancellationToken);
        if (existing is not null)
        {
            if (existing.GymCode is null && normalizedCode is not null)
            {
                existing.GymCode = normalizedCode;
                await _registry.SaveChangesAsync(cancellationToken);
            }
            return existing;
        }

        // SequentialNumber is an IDENTITY column on gyms_registry.gyms; SQL Server
        // assigns it on insert. Compute the prospective prefix from MAX()+1 (or 1)
        // and create schemas first so EF migrations can target them, then insert.
        var nextSeq = await _registry.Gyms.AnyAsync(cancellationToken)
            ? await _registry.Gyms.MaxAsync(g => g.SequentialNumber, cancellationToken) + 1
            : 1;

        var prefix = $"g{nextSeq}";
        SchemaCreator.ValidatePrefix(prefix);

        await _schemaCreator.CreateSchemasAsync(_registry, prefix, cancellationToken);

        var registration = new GymRegistration
        {
            GymId = gymId,
            OwnerId = ownerId,
            Name = name,
            Slug = slug,
            SchemaPrefix = prefix,
            // SequentialNumber omitted — IDENTITY column.
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            GymCode = normalizedCode
        };

        _registry.Gyms.Add(registration);
        await _registry.SaveChangesAsync(cancellationToken);

        // Run per-gym EF migrations so the g{N}_* tables exist (the schemas alone
        // are empty containers). Each per-gym DbContext is resolved inside a fresh
        // scope with the tenant accessor pointed at this prefix.
        await MigrateNewGymAsync(registration, cancellationToken);

        // Seed per-tenant billing reference data (plans/features/limits) and a
        // default subscription. Without this, IsFeatureEnabledAsync defaults to
        // "free" for every fresh gym because g{N}_billing.subscription_plans is empty.
        await HydrateBillingAsync(registration, cancellationToken);

        return registration;
    }

    public Task RehydrateBillingAsync(GymRegistration gym, CancellationToken cancellationToken = default)
        => HydrateBillingAsync(gym, cancellationToken);

    private async Task HydrateBillingAsync(GymRegistration gym, CancellationToken cancellationToken)
    {
        var conn = _registry.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        var prefix = gym.SchemaPrefix;
        SchemaCreator.ValidatePrefix(prefix);
        var sql = $@"
SET XACT_ABORT ON;
BEGIN TRAN;
-- Plans: insert missing, then sync editable fields so catalog edits in
-- billing.subscription_plans propagate to per-gym schemas. Code/Id are stable
-- keys (FKs depend on Id), so they're never overwritten.
INSERT INTO [{prefix}_billing].subscription_plans
  (Id, Code, Name, Description, MonthlyPriceEgp, AnnualPriceEgp, MemberLimit, TrainerLimit, StaffLimit, IsActive, IsPublic, IsTimeLimited, AvailableFrom, AvailableUntil, SortOrder, CreatedAt, UpdatedAt)
SELECT Id, Code, Name, Description, MonthlyPriceEgp, AnnualPriceEgp, MemberLimit, TrainerLimit, StaffLimit, IsActive, IsPublic, IsTimeLimited, AvailableFrom, AvailableUntil, SortOrder, CreatedAt, UpdatedAt
FROM billing.subscription_plans s
WHERE NOT EXISTS (SELECT 1 FROM [{prefix}_billing].subscription_plans g WHERE g.Id = s.Id);

UPDATE g
SET    g.Name = s.Name, g.Description = s.Description,
       g.MonthlyPriceEgp = s.MonthlyPriceEgp, g.AnnualPriceEgp = s.AnnualPriceEgp,
       g.MemberLimit = s.MemberLimit, g.TrainerLimit = s.TrainerLimit, g.StaffLimit = s.StaffLimit,
       g.IsActive = s.IsActive, g.IsPublic = s.IsPublic, g.IsTimeLimited = s.IsTimeLimited,
       g.AvailableFrom = s.AvailableFrom, g.AvailableUntil = s.AvailableUntil,
       g.SortOrder = s.SortOrder, g.UpdatedAt = SYSUTCDATETIME()
FROM   [{prefix}_billing].subscription_plans g
JOIN   billing.subscription_plans s ON s.Id = g.Id;

-- Features: insert missing, sync metadata.
INSERT INTO [{prefix}_billing].features (Id, Code, Name, Description, Category, IsActive, SortOrder, CreatedAt)
SELECT Id, Code, Name, Description, Category, IsActive, SortOrder, CreatedAt
FROM billing.features s
WHERE NOT EXISTS (SELECT 1 FROM [{prefix}_billing].features g WHERE g.Id = s.Id);

UPDATE g
SET    g.Name = s.Name, g.Description = s.Description, g.Category = s.Category,
       g.IsActive = s.IsActive, g.SortOrder = s.SortOrder
FROM   [{prefix}_billing].features g
JOIN   billing.features s ON s.Id = g.Id;

-- Plan-feature matrix: insert missing AND sync IsEnabled/LimitValue so toggling
-- a row in billing.plan_features (admin UI or BillingSeeder edit) flows down.
INSERT INTO [{prefix}_billing].plan_features (Id, PlanId, FeatureId, IsEnabled, LimitValue)
SELECT Id, PlanId, FeatureId, IsEnabled, LimitValue
FROM billing.plan_features s
WHERE NOT EXISTS (SELECT 1 FROM [{prefix}_billing].plan_features g WHERE g.PlanId = s.PlanId AND g.FeatureId = s.FeatureId);

UPDATE g
SET    g.IsEnabled = s.IsEnabled, g.LimitValue = s.LimitValue
FROM   [{prefix}_billing].plan_features g
JOIN   billing.plan_features s ON s.PlanId = g.PlanId AND s.FeatureId = g.FeatureId;

IF EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas sc ON sc.schema_id=t.schema_id WHERE sc.name='{prefix}_billing' AND t.name='plan_storage_limits')
BEGIN
    INSERT INTO [{prefix}_billing].plan_storage_limits (Id, PlanId, ResourceType, LimitValue)
    SELECT Id, PlanId, ResourceType, LimitValue
    FROM billing.plan_storage_limits s
    WHERE NOT EXISTS (SELECT 1 FROM [{prefix}_billing].plan_storage_limits g WHERE g.PlanId = s.PlanId AND g.ResourceType = s.ResourceType);

    UPDATE g
    SET    g.LimitValue = s.LimitValue
    FROM   [{prefix}_billing].plan_storage_limits g
    JOIN   billing.plan_storage_limits s ON s.PlanId = g.PlanId AND s.ResourceType = g.ResourceType;
END

DECLARE @planId uniqueidentifier;
DECLARE @ownerTier nvarchar(50) = (SELECT TOP 1 PlanTier FROM [identity].contracts WHERE OwnerId = (SELECT TOP 1 Id FROM [identity].owners WHERE UserId = @ownerUserId));
DECLARE @tierCode nvarchar(50) = LOWER(ISNULL(@ownerTier, N'free'));
-- Legacy admin DTOs expose ""Enterprise"" as a tier label. The actual plan
-- catalog uses ""business"" for the top tier; alias them so a gym whose owner
-- contract says ""Enterprise"" still resolves to the Business plan instead of
-- silently falling through to Free (which crushes the gym to a 1-trainer cap).
IF @tierCode = N'enterprise' SET @tierCode = N'business';
SELECT @planId = Id FROM [{prefix}_billing].subscription_plans WHERE IsActive = 1 AND Code = @tierCode;
IF @planId IS NULL
    SELECT @planId = Id FROM [{prefix}_billing].subscription_plans WHERE IsActive = 1 AND Code = N'free';

DECLARE @now datetime2 = SYSUTCDATETIME();
IF @planId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [{prefix}_billing].gym_subscriptions WHERE GymId = @gymId)
    BEGIN
        INSERT INTO [{prefix}_billing].gym_subscriptions
          (Id, GymId, PlanId, StartedAt, ExpiresAt, IsActive, IsTrial, TrialExpiresAt, PaymentStatus, LastPaymentAt, NextPaymentDue, OverdueSince, GracePeriodEndsAt)
        VALUES
          (NEWID(), @gymId, @planId, @now, DATEADD(YEAR, 1, @now), 1, 0, NULL, N'Paid', @now, DATEADD(MONTH, 1, @now), NULL, NULL);
    END
    ELSE
    BEGIN
        -- Repair orphan subscriptions: PlanId points to a wiped plan, sub is inactive,
        -- or expired. Without this, post-wipe boots would leave gyms stranded on a
        -- bad PlanId and IsFeatureEnabledAsync silently returns false.
        UPDATE s
        SET    PlanId        = @planId,
               IsActive      = 1,
               PaymentStatus = N'Paid',
               ExpiresAt     = DATEADD(YEAR, 1, @now),
               LastPaymentAt = @now,
               NextPaymentDue= DATEADD(MONTH, 1, @now),
               OverdueSince  = NULL,
               GracePeriodEndsAt = NULL
        FROM   [{prefix}_billing].gym_subscriptions s
        WHERE  s.GymId = @gymId
          AND  (NOT EXISTS (SELECT 1 FROM [{prefix}_billing].subscription_plans p WHERE p.Id = s.PlanId)
                OR s.IsActive = 0
                OR s.ExpiresAt < @now);
    END
END

COMMIT;";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var p1 = cmd.CreateParameter(); p1.ParameterName = "@gymId"; p1.Value = gym.GymId; cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "@ownerUserId"; p2.Value = gym.OwnerId; cmd.Parameters.Add(p2);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Provisioning {Prefix}: hydrated billing reference data + subscription", prefix);
    }

    private async Task MigrateNewGymAsync(GymRegistration gym, CancellationToken cancellationToken)
    {
        var perGymContextNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "GymsDbContext", "TrainingDbContext", "ProgressDbContext", "SchedulingDbContext",
            "NutritionDbContext", "CommunicationDbContext", "BillingDbContext",
            "GamificationDbContext", "OutboxDbContext"
        };

        var dbContextTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("GymApp") == true)
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .Where(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t) && perGymContextNames.Contains(t.Name))
            .ToList();

        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var accessor = sp.GetRequiredService<ITenantContextAccessor>();
        accessor.Set(gym.GymId, gym.SchemaPrefix, isOwnerScope: false);

        foreach (var ctxType in dbContextTypes)
        {
            try
            {
                var ctx = (DbContext?)sp.GetService(ctxType);
                if (ctx is null) continue;

                // EF's EnsureCreated short-circuits when the SQL Server database
                // already exists (which it always does in our shared-DB layout),
                // so we drop down to IRelationalDatabaseCreator.CreateTablesAsync
                // — that emits CREATE TABLE for every entity in the current model
                // (already resolved to `g{N}_*` via ITenantContext.SchemaFor).
                //
                // EF migrations baked the legacy `training` etc. schema names at
                // generation time; running them per-gym would write into the
                // wrong schema. Per-gym DDL via the model creator avoids that.
                // Schema evolution => coordinated CreateTables for the new
                // entity, applied across all registered gyms.
                // HasTables() checks the whole database, not this tenant's schema
                // (EF has no per-schema notion). Pull the schema name from the
                // tenant accessor and probe sys.tables directly. If empty, run
                // CreateTablesAsync, which emits CREATE TABLE for every entity
                // in the current model — schema-correct because OnModelCreating
                // already substituted the per-gym prefix.
                var conn = ctx.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync(cancellationToken);

                var defaultSchema = ctx.Model.GetDefaultSchema() ?? string.Empty;

                int tableCount;
                using (var probe = conn.CreateCommand())
                {
                    probe.CommandText =
                        "SELECT COUNT(*) FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = @schema;";
                    var p = probe.CreateParameter();
                    p.ParameterName = "@schema";
                    p.Value = defaultSchema;
                    probe.Parameters.Add(p);
                    tableCount = (int)(await probe.ExecuteScalarAsync(cancellationToken) ?? 0);
                }

                if (tableCount == 0)
                {
                    var creator = ctx.GetService<IRelationalDatabaseCreator>();
                    await creator.CreateTablesAsync(cancellationToken);
                    _logger.LogInformation("Provisioning {Prefix}: created tables for {Context}",
                        gym.SchemaPrefix, ctxType.Name);
                }
                else
                {
                    _logger.LogInformation("Provisioning {Prefix}: {Context} skipped (schema not empty)",
                        gym.SchemaPrefix, ctxType.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provisioning {Prefix}: failed to initialize {Context}", gym.SchemaPrefix, ctxType.Name);
                throw;
            }
        }
    }

    public async Task DeprovisionAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var registration = await _registry.Gyms.FirstOrDefaultAsync(g => g.GymId == gymId, cancellationToken);
        if (registration is null) return;

        await _schemaCreator.DropSchemasAsync(_registry, registration.SchemaPrefix, cancellationToken);

        _registry.Gyms.Remove(registration);
        await _registry.SaveChangesAsync(cancellationToken);
    }
}
