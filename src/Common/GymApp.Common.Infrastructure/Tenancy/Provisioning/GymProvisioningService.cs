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
        GymProvisioningRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = string.IsNullOrWhiteSpace(request.GymCode) ? null : request.GymCode.Trim().ToUpperInvariant();

        var existing = await _registry.Gyms.FirstOrDefaultAsync(g => g.GymId == request.GymId, cancellationToken);
        if (existing is not null)
        {
            await BackfillGymCodeAsync(existing, normalizedCode, cancellationToken);
            return existing;
        }

        var prefix = await AllocateSchemasAsync(cancellationToken);

        var registration = new GymRegistration
        {
            GymId = request.GymId,
            OwnerId = request.OwnerId,
            Name = request.Name,
            Slug = request.Slug,
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

    private async Task BackfillGymCodeAsync(GymRegistration existing, string? normalizedCode, CancellationToken cancellationToken)
    {
        if (existing.GymCode is not null || normalizedCode is null)
            return;

        existing.GymCode = normalizedCode;
        await _registry.SaveChangesAsync(cancellationToken);
    }

    // SequentialNumber is an IDENTITY column on gyms_registry.gyms; SQL Server
    // assigns it on insert. Compute the prospective prefix from MAX()+1 (or 1)
    // and create schemas first so EF migrations can target them, then insert.
    private async Task<string> AllocateSchemasAsync(CancellationToken cancellationToken)
    {
        var nextSeq = await _registry.Gyms.AnyAsync(cancellationToken)
            ? await _registry.Gyms.MaxAsync(g => g.SequentialNumber, cancellationToken) + 1
            : 1;

        var prefix = $"g{nextSeq}";
        SchemaCreator.ValidatePrefix(prefix);

        await _schemaCreator.CreateSchemasAsync(_registry, prefix, cancellationToken);
        return prefix;
    }

    private async Task HydrateBillingAsync(GymRegistration gym, CancellationToken cancellationToken)
    {
        var conn = await OpenConnectionAsync(_registry, cancellationToken);

        var prefix = gym.SchemaPrefix;
        SchemaCreator.ValidatePrefix(prefix);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = BillingHydrationSql.For(prefix);
        var p1 = cmd.CreateParameter(); p1.ParameterName = "@gymId"; p1.Value = gym.GymId; cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "@ownerUserId"; p2.Value = gym.OwnerId; cmd.Parameters.Add(p2);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Provisioning {Prefix}: hydrated billing reference data + subscription", prefix);
    }

    private async Task MigrateNewGymAsync(GymRegistration gym, CancellationToken cancellationToken)
    {
        var dbContextTypes = DiscoverPerGymContextTypes();

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

                await EnsureTablesAsync(ctx, ctxType, gym, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provisioning {Prefix}: failed to initialize {Context}", gym.SchemaPrefix, ctxType.Name);
                throw;
            }
        }
    }

    private static List<Type> DiscoverPerGymContextTypes()
    {
        var perGymContextNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "GymsDbContext", "TrainingDbContext", "ProgressDbContext", "SchedulingDbContext",
            "NutritionDbContext", "CommunicationDbContext", "BillingDbContext",
            "GamificationDbContext", "OutboxDbContext"
        };

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("GymApp") == true)
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .Where(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t) && perGymContextNames.Contains(t.Name))
            .ToList();
    }

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
    private async Task EnsureTablesAsync(DbContext ctx, Type ctxType, GymRegistration gym, CancellationToken cancellationToken)
    {
        var defaultSchema = ctx.Model.GetDefaultSchema() ?? string.Empty;
        var tableCount = await CountTablesAsync(ctx, defaultSchema, cancellationToken);

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

    private static async Task<int> CountTablesAsync(DbContext ctx, string schema, CancellationToken cancellationToken)
    {
        var conn = await OpenConnectionAsync(ctx, cancellationToken);

        using var probe = conn.CreateCommand();
        probe.CommandText =
            "SELECT COUNT(*) FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = @schema;";
        var p = probe.CreateParameter();
        p.ParameterName = "@schema";
        p.Value = schema;
        probe.Parameters.Add(p);
        return (int)(await probe.ExecuteScalarAsync(cancellationToken) ?? 0);
    }

    private static async Task<System.Data.Common.DbConnection> OpenConnectionAsync(DbContext ctx, CancellationToken cancellationToken)
    {
        var conn = ctx.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);
        return conn;
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
