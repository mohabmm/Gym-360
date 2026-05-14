using GymApp.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class GymStorageUsageConfiguration : IEntityTypeConfiguration<GymStorageUsage>
{
    public void Configure(EntityTypeBuilder<GymStorageUsage> builder)
    {
        builder.ToTable("gym_storage_usage");

        builder.HasKey(gsu => gsu.Id);

        builder.Property(gsu => gsu.Id)
            .ValueGeneratedNever();

        builder.Property(gsu => gsu.GymId)
            .IsRequired();

        builder.Property(gsu => gsu.ResourceType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(gsu => gsu.CurrentUsage)
            .IsRequired();

        builder.Property(gsu => gsu.LastCalculatedAt)
            .IsRequired();

        builder.HasIndex(gsu => new { gsu.GymId, gsu.ResourceType })
            .IsUnique();
    }
}
