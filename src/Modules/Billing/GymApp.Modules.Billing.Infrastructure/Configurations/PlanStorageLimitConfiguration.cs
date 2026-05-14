using GymApp.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class PlanStorageLimitConfiguration : IEntityTypeConfiguration<PlanStorageLimit>
{
    public void Configure(EntityTypeBuilder<PlanStorageLimit> builder)
    {
        builder.ToTable("plan_storage_limits");

        builder.HasKey(psl => psl.Id);

        builder.Property(psl => psl.Id)
            .ValueGeneratedNever();

        builder.Property(psl => psl.PlanId)
            .IsRequired();

        builder.Property(psl => psl.ResourceType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(psl => psl.LimitValue)
            .IsRequired();

        builder.HasIndex(psl => new { psl.PlanId, psl.ResourceType })
            .IsUnique();
    }
}
