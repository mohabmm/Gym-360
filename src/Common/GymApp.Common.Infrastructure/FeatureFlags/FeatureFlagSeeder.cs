using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Infrastructure.FeatureFlags.Persistence;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GymApp.Common.Infrastructure.FeatureFlags;

/// <summary>
/// Startup sync from code to database. Idempotent and additive:
/// <list type="bullet">
/// <item>Upserts <see cref="FeatureRegistry"/> into <c>features</c>; keys removed from code are marked inactive, never deleted.</item>
/// <item>Creates the <c>pt_module</c> and <c>business</c> presets and adds a preset value for any new key.
/// Existing preset values are never changed here — the dashboard owns them.</item>
/// <item>Assigns every gym that has no preset to the default preset (<c>pt_module</c>), with an audit row.</item>
/// </list>
/// </summary>
public static class FeatureFlagSeeder
{
    private static readonly Guid SystemActor = Guid.Empty;

    private sealed record PresetSeed(string Code, string NameEn, string NameAr, string? LimitsPlanCode, Func<FeatureDefinition, bool> Value);

    private static readonly PresetSeed[] Presets =
    {
        // Pilot PT gyms get Business seat limits so a multi-coach PT gym is never blocked by the Free plan.
        new(FeatureRegistry.PtModulePreset, "PT module", "وحدة التدريب الشخصي", "business", d => d.InPtModule),
        new(FeatureRegistry.BusinessPreset, "Business (all features)", "الأعمال (كل الميزات)", null, d => d.InBusiness),
    };

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<FeatureFlagsDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(FeatureFlagSeeder));
        var defaultPreset = sp.GetRequiredService<IOptions<FeatureFlagOptions>>().Value.DefaultPreset;

        await SyncFeaturesAsync(db, cancellationToken);
        await SyncPresetsAsync(db, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var assigned = await AssignUnassignedGymsAsync(db, sp.GetRequiredService<TenantRegistryDbContext>(), defaultPreset, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        if (assigned > 0)
        {
            // Resolution may already be cached from before the assignment.
            await sp.GetRequiredService<FeatureFlagCache>()
                .InvalidateAsync(await db.GymPresets.Select(g => g.GymId).ToListAsync(cancellationToken), cancellationToken);
        }

        logger.LogInformation(
            "FeatureFlagSeeder: {Features} features, {Assigned} gyms assigned to {Preset}",
            FeatureRegistry.All.Count, assigned, defaultPreset);
    }

    private static async Task SyncFeaturesAsync(FeatureFlagsDbContext db, CancellationToken cancellationToken)
    {
        var existing = await db.Features.ToDictionaryAsync(f => f.Key, cancellationToken);
        var now = DateTime.UtcNow;

        for (var i = 0; i < FeatureRegistry.All.Count; i++)
        {
            var def = FeatureRegistry.All[i];
            if (!existing.TryGetValue(def.Key, out var row))
            {
                row = new FeatureRow { Key = def.Key };
                db.Features.Add(row);
            }

            row.Module = def.Module;
            row.Kind = def.Kind == FeatureKind.Setting ? "setting" : "feature";
            row.NameEn = def.Text.NameEn;
            row.NameAr = def.Text.NameAr;
            row.DescriptionEn = def.Text.DescriptionEn;
            row.DescriptionAr = def.Text.DescriptionAr;
            row.Apps = string.Join(',', FeatureCatalog.AppNames(def.Apps));
            row.SortOrder = i;
            row.IsActive = true;
            row.UpdatedAtUtc = now;
        }

        foreach (var stale in existing.Values.Where(f => !FeatureRegistry.IsKnown(f.Key)))
            stale.IsActive = false;
    }

    private static async Task SyncPresetsAsync(FeatureFlagsDbContext db, CancellationToken cancellationToken)
    {
        var presets = await db.Presets.ToDictionaryAsync(p => p.Code, cancellationToken);
        var values = await db.PresetFeatures.ToListAsync(cancellationToken);

        foreach (var seed in Presets)
        {
            if (!presets.ContainsKey(seed.Code))
            {
                db.Presets.Add(new PresetRow
                {
                    Code = seed.Code,
                    NameEn = seed.NameEn,
                    NameAr = seed.NameAr,
                    LimitsPlanCode = seed.LimitsPlanCode,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }

            var missing = FeatureRegistry.All
                .Where(d => !values.Any(v => v.PresetCode == seed.Code && v.FeatureKey == d.Key));
            foreach (var def in missing)
                db.PresetFeatures.Add(new PresetFeatureRow { PresetCode = seed.Code, FeatureKey = def.Key, Enabled = seed.Value(def) });
        }
    }

    private static async Task<int> AssignUnassignedGymsAsync(
        FeatureFlagsDbContext db,
        TenantRegistryDbContext registry,
        string defaultPreset,
        CancellationToken cancellationToken)
    {
        List<Guid> gymIds;
        try
        {
            gymIds = await registry.Gyms.AsNoTracking().Select(g => g.GymId).ToListAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Registry not created yet (fresh DB in soft mode): nothing to assign.
            return 0;
        }

        var assigned = (await db.GymPresets.Select(g => g.GymId).ToListAsync(cancellationToken)).ToHashSet();
        var now = DateTime.UtcNow;
        var count = 0;

        foreach (var gymId in gymIds.Where(id => !assigned.Contains(id)))
        {
            db.GymPresets.Add(new GymPresetRow { GymId = gymId, PresetCode = defaultPreset, UpdatedAtUtc = now, UpdatedBy = SystemActor });
            db.AuditLog.Add(new FlagAuditRow
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = now,
                ActorUserId = SystemActor,
                ActorEmail = "system",
                GymId = gymId,
                ChangeType = FlagChangeTypes.PresetAssigned,
                PresetCode = defaultPreset,
                OldValue = null,
                NewValue = defaultPreset,
                Reason = "Initial assignment (feature flags rollout)",
            });
            count++;
        }

        return count;
    }
}
