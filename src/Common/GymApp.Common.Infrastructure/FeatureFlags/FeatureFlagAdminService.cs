using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Domain;
using GymApp.Common.Infrastructure.FeatureFlags.Persistence;
using GymApp.Common.Infrastructure.Tenancy.Registry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GymApp.Common.Infrastructure.FeatureFlags;

internal sealed class FeatureFlagAdminService : IFeatureFlagAdminService
{
    private const int MaxPageSize = 200;

    private readonly FeatureFlagsDbContext _db;
    private readonly TenantRegistryDbContext _registry;
    private readonly FeatureFlagChangePublisher _publisher;
    private readonly FeatureFlagOptions _options;

    public FeatureFlagAdminService(
        FeatureFlagsDbContext db,
        TenantRegistryDbContext registry,
        FeatureFlagChangePublisher publisher,
        IOptions<FeatureFlagOptions> options)
    {
        _db = db;
        _registry = registry;
        _publisher = publisher;
        _options = options.Value;
    }

    public IReadOnlyList<FeatureCatalogItem> GetCatalog() =>
        FeatureRegistry.All.Select(FeatureCatalog.ToItem).ToList();

    public async Task<FlagPage<GymFlagSummary>> ListGymsAsync(GymListQuery query, CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = Clamp(query);
        var gyms = await _registry.Gyms.AsNoTracking()
            .Select(g => new { g.GymId, g.Name, g.Status })
            .ToListAsync(cancellationToken);

        var presets = await _db.GymPresets.AsNoTracking()
            .ToDictionaryAsync(g => g.GymId, g => g.PresetCode, cancellationToken);
        var overrideCounts = await _db.GymOverrides.AsNoTracking()
            .GroupBy(o => o.GymId)
            .Select(g => new { GymId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GymId, x => x.Count, cancellationToken);

        var rows = gyms
            .Select(g => new GymFlagSummary(
                g.GymId,
                g.Name,
                g.Status,
                presets.GetValueOrDefault(g.GymId) ?? _options.DefaultPreset,
                overrideCounts.GetValueOrDefault(g.GymId)))
            .Where(g => MatchesSearch(g, query))
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var items = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new FlagPage<GymFlagSummary>(items, page, pageSize, rows.Count);
    }

    public async Task<Result<GymFlagDetail>> GetGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var gym = await FindGymAsync(gymId, cancellationToken);
        if (gym is null)
            return Result.Failure<GymFlagDetail>(FeatureFlagErrors.GymNotFound);

        var presetCode = await GetGymPresetCodeAsync(gymId, cancellationToken);
        var presetValues = await LoadPresetValuesAsync(presetCode, cancellationToken);
        var overrides = await _db.GymOverrides.AsNoTracking()
            .Where(o => o.GymId == gymId)
            .ToDictionaryAsync(o => o.FeatureKey, cancellationToken);

        var rows = FeatureRegistry.All
            .Select(def => ToGymRow(def, presetValues, overrides))
            .ToList();

        return Result.Success(new GymFlagDetail(gymId, gym.Name, presetCode, rows));
    }

    public async Task<Result<int>> SetGymPresetAsync(
        IReadOnlyCollection<Guid> gymIds, string presetCode, AdminActor actor, CancellationToken cancellationToken = default)
    {
        if (!await _db.Presets.AnyAsync(p => p.Code == presetCode, cancellationToken))
            return Result.Failure<int>(FeatureFlagErrors.UnknownPreset);

        var knownGymIds = await _registry.Gyms.AsNoTracking()
            .Where(g => gymIds.Contains(g.GymId))
            .Select(g => g.GymId)
            .ToListAsync(cancellationToken);
        if (knownGymIds.Count != gymIds.Distinct().Count())
            return Result.Failure<int>(FeatureFlagErrors.GymNotFound);

        var existing = await _db.GymPresets
            .Where(g => knownGymIds.Contains(g.GymId))
            .ToDictionaryAsync(g => g.GymId, cancellationToken);

        var change = new PresetAssignment(presetCode, actor);
        var changed = new List<Guid>();
        foreach (var gymId in knownGymIds)
        {
            if (AssignPreset(gymId, existing.GetValueOrDefault(gymId), change))
                changed.Add(gymId);
        }

        await SaveAndPublishAsync(changed, cancellationToken);
        return Result.Success(changed.Count);
    }

    public async Task<Result> SetOverrideAsync(
        Guid gymId, FlagOverrideChange change, AdminActor actor, CancellationToken cancellationToken = default)
    {
        if (!FeatureRegistry.IsKnown(change.FeatureKey))
            return Result.Failure(FeatureFlagErrors.UnknownFeature);
        if (await FindGymAsync(gymId, cancellationToken) is null)
            return Result.Failure(FeatureFlagErrors.GymNotFound);

        var row = await _db.GymOverrides
            .FirstOrDefaultAsync(o => o.GymId == gymId && o.FeatureKey == change.FeatureKey, cancellationToken);
        var oldValue = row is null
            ? await DescribeEffectiveAsync(gymId, change, cancellationToken)
            : Label(FlagSource.Override, row.Enabled);

        if (row is null)
        {
            row = new GymOverrideRow { GymId = gymId, FeatureKey = change.FeatureKey };
            _db.GymOverrides.Add(row);
        }

        row.Enabled = change.Enabled;
        row.Reason = change.Reason;
        row.SetBy = actor.UserId;
        row.SetAtUtc = DateTime.UtcNow;

        _db.AuditLog.Add(Audit(actor, FlagChangeTypes.OverrideSet, gymId) with
        {
            FeatureKey = change.FeatureKey,
            OldValue = oldValue,
            NewValue = Label(FlagSource.Override, change.Enabled),
            Reason = change.Reason,
        });

        await SaveAndPublishAsync(new[] { gymId }, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ClearOverrideAsync(
        Guid gymId, string featureKey, AdminActor actor, CancellationToken cancellationToken = default)
    {
        var row = await _db.GymOverrides
            .FirstOrDefaultAsync(o => o.GymId == gymId && o.FeatureKey == featureKey, cancellationToken);
        if (row is null)
            return Result.Success();

        _db.GymOverrides.Remove(row);
        var presetCode = await GetGymPresetCodeAsync(gymId, cancellationToken);
        var presetValues = await LoadPresetValuesAsync(presetCode, cancellationToken);

        _db.AuditLog.Add(Audit(actor, FlagChangeTypes.OverrideCleared, gymId) with
        {
            FeatureKey = featureKey,
            PresetCode = presetCode,
            OldValue = Label(FlagSource.Override, row.Enabled),
            NewValue = Label(FlagSource.Preset, presetValues.GetValueOrDefault(featureKey)),
        });

        await SaveAndPublishAsync(new[] { gymId }, cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<PresetDetail>> ListPresetsAsync(CancellationToken cancellationToken = default)
    {
        var presets = await _db.Presets.AsNoTracking().OrderBy(p => p.Code).ToListAsync(cancellationToken);
        var values = await _db.PresetFeatures.AsNoTracking().ToListAsync(cancellationToken);
        var gymCounts = await CountGymsPerPresetAsync(cancellationToken);

        return presets
            .Select(p => new PresetDetail(
                p.Code,
                p.NameEn,
                p.NameAr,
                gymCounts.GetValueOrDefault(p.Code),
                FeatureRegistry.All.ToDictionary(
                    d => d.Key,
                    d => values.Any(v => v.PresetCode == p.Code && v.FeatureKey == d.Key && v.Enabled))))
            .ToList();
    }

    public async Task<Result<int>> UpdatePresetAsync(
        string presetCode, IReadOnlyDictionary<string, bool> values, AdminActor actor, CancellationToken cancellationToken = default)
    {
        if (!await _db.Presets.AnyAsync(p => p.Code == presetCode, cancellationToken))
            return Result.Failure<int>(FeatureFlagErrors.UnknownPreset);
        if (values.Keys.Any(k => !FeatureRegistry.IsKnown(k)))
            return Result.Failure<int>(FeatureFlagErrors.UnknownFeature);

        var rows = await _db.PresetFeatures
            .Where(p => p.PresetCode == presetCode)
            .ToDictionaryAsync(p => p.FeatureKey, cancellationToken);

        var changes = 0;
        foreach (var (key, enabled) in values)
            changes += ApplyPresetValue(new PresetEdit(presetCode, key, enabled), rows.GetValueOrDefault(key), actor) ? 1 : 0;

        if (changes == 0)
            return Result.Success(0);

        var affected = await GymIdsOnPresetAsync(presetCode, cancellationToken);
        await SaveAndPublishAsync(affected, cancellationToken);
        return Result.Success(affected.Count);
    }

    public async Task<FlagPage<FlagAuditEntry>> GetAuditAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = Clamp(query);
        var q = _db.AuditLog.AsNoTracking().AsQueryable();
        if (query.GymId.HasValue)
            q = q.Where(a => a.GymId == query.GymId);
        if (!string.IsNullOrWhiteSpace(query.FeatureKey))
            q = q.Where(a => a.FeatureKey == query.FeatureKey);

        var total = await q.CountAsync(cancellationToken);
        var rows = await q.OrderByDescending(a => a.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var gymNames = await _registry.Gyms.AsNoTracking()
            .ToDictionaryAsync(g => g.GymId, g => g.Name, cancellationToken);

        var items = rows.Select(a => new FlagAuditEntry(
                a.Id, a.OccurredAtUtc, a.ActorUserId, a.ActorEmail, a.GymId,
                a.GymId.HasValue ? gymNames.GetValueOrDefault(a.GymId.Value) : null,
                a.ChangeType, a.FeatureKey, a.PresetCode, a.OldValue, a.NewValue, a.Reason))
            .ToList();

        return new FlagPage<FlagAuditEntry>(items, page, pageSize, total);
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>One flag value an admin sets on a preset.</summary>
    private sealed record PresetEdit(string PresetCode, string FeatureKey, bool Enabled);

    private bool ApplyPresetValue(PresetEdit edit, PresetFeatureRow? row, AdminActor actor)
    {
        var old = row?.Enabled ?? false;
        if (row is not null && old == edit.Enabled)
            return false;

        if (row is null)
            _db.PresetFeatures.Add(new PresetFeatureRow { PresetCode = edit.PresetCode, FeatureKey = edit.FeatureKey, Enabled = edit.Enabled });
        else
            row.Enabled = edit.Enabled;

        _db.AuditLog.Add(Audit(actor, FlagChangeTypes.PresetEdited, gymId: null) with
        {
            PresetCode = edit.PresetCode,
            FeatureKey = edit.FeatureKey,
            OldValue = Label(FlagSource.Preset, old),
            NewValue = Label(FlagSource.Preset, edit.Enabled),
        });
        return true;
    }

    /// <summary>A preset an admin puts gyms on.</summary>
    private sealed record PresetAssignment(string PresetCode, AdminActor Actor);

    /// <summary>Puts one gym on the preset and audits it; false when it already was on it.</summary>
    private bool AssignPreset(Guid gymId, GymPresetRow? row, PresetAssignment change)
    {
        var oldCode = row?.PresetCode ?? _options.DefaultPreset;
        if (row?.PresetCode == change.PresetCode)
            return false;

        if (row is null)
        {
            row = new GymPresetRow { GymId = gymId };
            _db.GymPresets.Add(row);
        }

        row.PresetCode = change.PresetCode;
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.UpdatedBy = change.Actor.UserId;
        _db.AuditLog.Add(Audit(change.Actor, FlagChangeTypes.PresetAssigned, gymId) with
        {
            PresetCode = change.PresetCode,
            OldValue = oldCode,
            NewValue = change.PresetCode,
        });
        return true;
    }

    private async Task SaveAndPublishAsync(IReadOnlyCollection<Guid> gymIds, CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
        await _publisher.PublishAsync(gymIds, cancellationToken);
    }

    private async Task<string> DescribeEffectiveAsync(Guid gymId, FlagOverrideChange change, CancellationToken cancellationToken)
    {
        var presetCode = await GetGymPresetCodeAsync(gymId, cancellationToken);
        var values = await LoadPresetValuesAsync(presetCode, cancellationToken);
        return Label(FlagSource.Preset, values.GetValueOrDefault(change.FeatureKey));
    }

    private async Task<string> GetGymPresetCodeAsync(Guid gymId, CancellationToken cancellationToken) =>
        await _db.GymPresets.AsNoTracking()
            .Where(g => g.GymId == gymId)
            .Select(g => g.PresetCode)
            .FirstOrDefaultAsync(cancellationToken) ?? _options.DefaultPreset;

    private Task<Dictionary<string, bool>> LoadPresetValuesAsync(string presetCode, CancellationToken cancellationToken) =>
        _db.PresetFeatures.AsNoTracking()
            .Where(p => p.PresetCode == presetCode)
            .ToDictionaryAsync(p => p.FeatureKey, p => p.Enabled, cancellationToken);

    private async Task<Dictionary<string, int>> CountGymsPerPresetAsync(CancellationToken cancellationToken)
    {
        var allGymIds = await _registry.Gyms.AsNoTracking().Select(g => g.GymId).ToListAsync(cancellationToken);
        var assigned = await _db.GymPresets.AsNoTracking()
            .ToDictionaryAsync(g => g.GymId, g => g.PresetCode, cancellationToken);

        return allGymIds
            .GroupBy(id => assigned.GetValueOrDefault(id) ?? _options.DefaultPreset)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task<IReadOnlyCollection<Guid>> GymIdsOnPresetAsync(string presetCode, CancellationToken cancellationToken)
    {
        var allGymIds = await _registry.Gyms.AsNoTracking().Select(g => g.GymId).ToListAsync(cancellationToken);
        var assigned = await _db.GymPresets.AsNoTracking()
            .ToDictionaryAsync(g => g.GymId, g => g.PresetCode, cancellationToken);

        return allGymIds
            .Where(id => (assigned.GetValueOrDefault(id) ?? _options.DefaultPreset) == presetCode)
            .ToList();
    }

    private Task<Common.Application.Tenancy.GymRegistration?> FindGymAsync(Guid gymId, CancellationToken cancellationToken) =>
        _registry.Gyms.AsNoTracking().FirstOrDefaultAsync(g => g.GymId == gymId, cancellationToken);

    private static GymFlagRow ToGymRow(
        FeatureDefinition def,
        IReadOnlyDictionary<string, bool> presetValues,
        IReadOnlyDictionary<string, GymOverrideRow> overrides)
    {
        var overrideRow = overrides.GetValueOrDefault(def.Key);
        var overrideValues = overrideRow is null
            ? new Dictionary<string, bool>()
            : new Dictionary<string, bool> { [def.Key] = overrideRow.Enabled };
        var resolved = FeatureResolver.ResolveOne(def.Key, presetValues, overrideValues);

        return new GymFlagRow(
            FeatureCatalog.ToItem(def),
            resolved.Enabled,
            resolved.Source == FlagSource.Override ? "override" : "preset",
            resolved.PresetValue,
            overrideRow is not null,
            overrideRow?.Reason,
            overrideRow?.SetAtUtc);
    }

    private static bool MatchesSearch(GymFlagSummary gym, GymListQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.PresetCode) && gym.PresetCode != query.PresetCode)
            return false;

        return string.IsNullOrWhiteSpace(query.Search)
            || gym.Name.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static FlagAuditRow Audit(AdminActor actor, string changeType, Guid? gymId) => new()
    {
        Id = Guid.NewGuid(),
        OccurredAtUtc = DateTime.UtcNow,
        ActorUserId = actor.UserId,
        ActorEmail = actor.Email,
        GymId = gymId,
        ChangeType = changeType,
    };

    /// <summary>Audit wording, e.g. "on (override)" or "off (preset)".</summary>
    private static string Label(FlagSource source, bool enabled) =>
        $"{(enabled ? "on" : "off")} ({(source == FlagSource.Override ? "override" : "preset")})";

    private static (int Page, int PageSize) Clamp(IFlagPageRequest request) =>
        (Math.Max(1, request.Page), Math.Clamp(request.PageSize, 1, MaxPageSize));
}
