using GymApp.Common.Domain;

namespace GymApp.Common.Application.FeatureFlags;

/// <summary>Super Admin operations on presets, per-gym presets and overrides. Every change is audited.</summary>
public interface IFeatureFlagAdminService
{
    Task<FlagPage<GymFlagSummary>> ListGymsAsync(GymListQuery query, CancellationToken cancellationToken = default);

    Task<Result<GymFlagDetail>> GetGymAsync(Guid gymId, CancellationToken cancellationToken = default);

    /// <summary>Assigns a preset to one or more gyms (bulk action). Overrides are kept.</summary>
    Task<Result<int>> SetGymPresetAsync(
        IReadOnlyCollection<Guid> gymIds, string presetCode, AdminActor actor, CancellationToken cancellationToken = default);

    Task<Result> SetOverrideAsync(
        Guid gymId, FlagOverrideChange change, AdminActor actor, CancellationToken cancellationToken = default);

    /// <summary>"Reset to preset": removes the override so the preset value applies again.</summary>
    Task<Result> ClearOverrideAsync(
        Guid gymId, string featureKey, AdminActor actor, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PresetDetail>> ListPresetsAsync(CancellationToken cancellationToken = default);

    Task<Result<int>> UpdatePresetAsync(
        string presetCode, IReadOnlyDictionary<string, bool> values, AdminActor actor, CancellationToken cancellationToken = default);

    Task<FlagPage<FlagAuditEntry>> GetAuditAsync(AuditQuery query, CancellationToken cancellationToken = default);

    IReadOnlyList<FeatureCatalogItem> GetCatalog();
}

public sealed record AdminActor(Guid UserId, string? Email);

public sealed record FlagOverrideChange(string FeatureKey, bool Enabled, string? Reason);

/// <summary>A paged admin list request.</summary>
public interface IFlagPageRequest
{
    int Page { get; }

    int PageSize { get; }
}

public sealed record GymListQuery(string? Search, string? PresetCode, int Page = 1, int PageSize = 50) : IFlagPageRequest;

public sealed record AuditQuery(Guid? GymId, string? FeatureKey, int Page = 1, int PageSize = 50) : IFlagPageRequest;

public sealed record FlagPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record GymFlagSummary(Guid GymId, string Name, string Status, string PresetCode, int OverrideCount);

public sealed record FeatureCatalogItem(
    string Key,
    string Module,
    string Kind,
    string NameEn,
    string NameAr,
    string DescriptionEn,
    string DescriptionAr,
    IReadOnlyList<string> Apps);

public sealed record GymFlagRow(
    FeatureCatalogItem Feature,
    bool Enabled,
    string Source,
    bool PresetValue,
    bool HasOverride,
    string? OverrideReason,
    DateTime? OverrideSetAtUtc);

public sealed record GymFlagDetail(
    Guid GymId,
    string Name,
    string PresetCode,
    IReadOnlyList<GymFlagRow> Flags);

public sealed record PresetDetail(
    string Code,
    string NameEn,
    string NameAr,
    int GymCount,
    IReadOnlyDictionary<string, bool> Values);

public sealed record FlagAuditEntry(
    Guid Id,
    DateTime OccurredAtUtc,
    Guid ActorUserId,
    string? ActorEmail,
    Guid? GymId,
    string? GymName,
    string ChangeType,
    string? FeatureKey,
    string? PresetCode,
    string? OldValue,
    string? NewValue,
    string? Reason);
