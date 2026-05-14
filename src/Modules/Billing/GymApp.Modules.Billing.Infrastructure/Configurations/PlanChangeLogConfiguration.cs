using GymApp.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class PlanChangeLogConfiguration : IEntityTypeConfiguration<PlanChangeLog>
{
    public void Configure(EntityTypeBuilder<PlanChangeLog> builder)
    {
        builder.ToTable("plan_change_log");

        builder.HasKey(pcl => pcl.Id);

        builder.Property(pcl => pcl.Id)
            .ValueGeneratedNever();

        builder.Property(pcl => pcl.GymId);

        builder.Property(pcl => pcl.ChangeType)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(pcl => pcl.EntityType)
            .HasMaxLength(20);

        builder.Property(pcl => pcl.EntityId);

        builder.Property(pcl => pcl.OldValue)
            .HasMaxLength(500);

        builder.Property(pcl => pcl.NewValue)
            .HasMaxLength(500);

        builder.Property(pcl => pcl.Reason)
            .HasMaxLength(500);

        builder.Property(pcl => pcl.ChangedBy)
            .IsRequired();

        builder.Property(pcl => pcl.ChangedAt)
            .IsRequired();

        builder.HasIndex(pcl => pcl.GymId);
        builder.HasIndex(pcl => pcl.ChangedAt);
    }
}
