using Microsoft.EntityFrameworkCore;

namespace GymApp.Common.Infrastructure.FeatureFlags.Persistence;

/// <summary>
/// Cross-tenant (global) store for feature flags, schema <c>feature_flags</c>. Like the gym
/// registry it is never per-gym: presets and the audit log span all gyms.
/// </summary>
public sealed class FeatureFlagsDbContext : DbContext
{
    public const string SchemaName = "feature_flags";

    public FeatureFlagsDbContext(DbContextOptions<FeatureFlagsDbContext> options) : base(options) { }

    public DbSet<FeatureRow> Features => Set<FeatureRow>();
    public DbSet<PresetRow> Presets => Set<PresetRow>();
    public DbSet<PresetFeatureRow> PresetFeatures => Set<PresetFeatureRow>();
    public DbSet<GymPresetRow> GymPresets => Set<GymPresetRow>();
    public DbSet<GymOverrideRow> GymOverrides => Set<GymOverrideRow>();
    public DbSet<FlagAuditRow> AuditLog => Set<FlagAuditRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<FeatureRow>(e =>
        {
            e.ToTable("features");
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(100);
            e.Property(x => x.Module).HasMaxLength(50).IsRequired();
            e.Property(x => x.Kind).HasMaxLength(20).IsRequired();
            e.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
            e.Property(x => x.DescriptionEn).HasMaxLength(1000).IsRequired();
            e.Property(x => x.DescriptionAr).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Apps).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<PresetRow>(e =>
        {
            e.ToTable("presets");
            e.HasKey(x => x.Code);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
            e.Property(x => x.LimitsPlanCode).HasMaxLength(50);
        });

        modelBuilder.Entity<PresetFeatureRow>(e =>
        {
            e.ToTable("preset_features");
            e.HasKey(x => new { x.PresetCode, x.FeatureKey });
            e.Property(x => x.PresetCode).HasMaxLength(50);
            e.Property(x => x.FeatureKey).HasMaxLength(100);
        });

        modelBuilder.Entity<GymPresetRow>(e =>
        {
            e.ToTable("gym_presets");
            e.HasKey(x => x.GymId);
            e.Property(x => x.GymId).ValueGeneratedNever();
            e.Property(x => x.PresetCode).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.PresetCode);
        });

        modelBuilder.Entity<GymOverrideRow>(e =>
        {
            e.ToTable("gym_overrides");
            e.HasKey(x => new { x.GymId, x.FeatureKey });
            e.Property(x => x.FeatureKey).HasMaxLength(100);
            e.Property(x => x.Reason).HasMaxLength(500);
        });

        modelBuilder.Entity<FlagAuditRow>(e =>
        {
            e.ToTable("audit_log");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.ActorEmail).HasMaxLength(256);
            e.Property(x => x.ChangeType).HasMaxLength(50).IsRequired();
            e.Property(x => x.FeatureKey).HasMaxLength(100);
            e.Property(x => x.PresetCode).HasMaxLength(50);
            e.Property(x => x.OldValue).HasMaxLength(100);
            e.Property(x => x.NewValue).HasMaxLength(100);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.HasIndex(x => x.OccurredAtUtc);
            e.HasIndex(x => new { x.GymId, x.OccurredAtUtc });
            e.HasIndex(x => new { x.FeatureKey, x.OccurredAtUtc });
        });
    }
}
