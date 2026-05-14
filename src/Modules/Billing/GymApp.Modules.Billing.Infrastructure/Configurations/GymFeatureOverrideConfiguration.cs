using GymApp.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class GymFeatureOverrideConfiguration : IEntityTypeConfiguration<GymFeatureOverride>
{
    public void Configure(EntityTypeBuilder<GymFeatureOverride> builder)
    {
        builder.ToTable("gym_feature_overrides");

        builder.HasKey(gfo => gfo.Id);

        builder.Property(gfo => gfo.Id)
            .ValueGeneratedNever();

        builder.Property(gfo => gfo.GymId)
            .IsRequired();

        builder.Property(gfo => gfo.FeatureId)
            .IsRequired();

        builder.Property(gfo => gfo.OverrideValue)
            .IsRequired();

        builder.Property(gfo => gfo.Reason)
            .HasMaxLength(500);

        builder.Property(gfo => gfo.SetBy)
            .IsRequired();

        builder.Property(gfo => gfo.SetAt)
            .IsRequired();

        builder.HasIndex(gfo => new { gfo.GymId, gfo.FeatureId })
            .IsUnique();
    }
}
