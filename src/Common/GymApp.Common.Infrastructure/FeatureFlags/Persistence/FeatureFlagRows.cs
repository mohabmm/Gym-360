namespace GymApp.Common.Infrastructure.FeatureFlags.Persistence;

/// <summary>Registry entry mirrored from code (<c>FeatureRegistry</c>) for the dashboard and reporting.</summary>
public sealed class FeatureRow
{
    public string Key { get; set; } = default!;
    public string Module { get; set; } = default!;
    public string Kind { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string DescriptionEn { get; set; } = default!;
    public string DescriptionAr { get; set; } = default!;
    public string Apps { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>A named set of flag values (pt_module, business).</summary>
public sealed class PresetRow
{
    public string Code { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;

    /// <summary>Optional subscription plan code whose seat/storage limits gyms on this preset use.</summary>
    public string? LimitsPlanCode { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public sealed class PresetFeatureRow
{
    public string PresetCode { get; set; } = default!;
    public string FeatureKey { get; set; } = default!;
    public bool Enabled { get; set; }
}

public sealed class GymPresetRow
{
    public Guid GymId { get; set; }
    public string PresetCode { get; set; } = default!;
    public DateTime UpdatedAtUtc { get; set; }
    public Guid UpdatedBy { get; set; }
}

public sealed class GymOverrideRow
{
    public Guid GymId { get; set; }
    public string FeatureKey { get; set; } = default!;
    public bool Enabled { get; set; }
    public string? Reason { get; set; }
    public Guid SetBy { get; set; }
    public DateTime SetAtUtc { get; set; }
}

/// <summary>Append-only audit of every flag change: who, when, gym, flag or preset, old → new.</summary>
public sealed record FlagAuditRow
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public Guid? GymId { get; set; }
    public string ChangeType { get; set; } = default!;
    public string? FeatureKey { get; set; }
    public string? PresetCode { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
}

public static class FlagChangeTypes
{
    public const string PresetAssigned = "preset_assigned";
    public const string OverrideSet = "override_set";
    public const string OverrideCleared = "override_cleared";
    public const string PresetEdited = "preset_edited";
}
