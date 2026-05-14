namespace GymApp.Common.Application.Tenancy;

public sealed class GymRegistration
{
    public Guid GymId { get; set; }

    public Guid OwnerId { get; set; }

    public string Name { get; set; } = default!;

    public string Slug { get; set; } = default!;

    /// <summary>
    /// Numeric per-gym schema prefix, e.g. "g1", "g2". Validated regex `^g\d+$`
    /// before insert to prevent dynamic-SQL injection on schema names.
    /// </summary>
    public string SchemaPrefix { get; set; } = default!;

    public int SequentialNumber { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Public 6-char gym code denormalized from `g{N}_gyms.gyms.GymCode`. Avoids
    /// a per-gym SQL walk in `IGymRegistry.FindByCodeAsync`. Nullable so legacy
    /// rows pre-denormalization still resolve via fallback walker.
    /// </summary>
    public string? GymCode { get; set; }
}
