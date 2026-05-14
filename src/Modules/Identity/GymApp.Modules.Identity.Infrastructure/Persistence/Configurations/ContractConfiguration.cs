using GymApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.OwnerId).IsRequired();
        builder.HasIndex(c => c.OwnerId).IsUnique();

        builder.Property(c => c.DashboardMode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.PlanTier).HasMaxLength(50).IsRequired();
        builder.Property(c => c.EffectiveFrom).IsRequired();
        builder.Property(c => c.EffectiveTo);
    }
}
