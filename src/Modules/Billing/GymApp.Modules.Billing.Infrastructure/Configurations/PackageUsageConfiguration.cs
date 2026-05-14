using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class PackageUsageConfiguration : IEntityTypeConfiguration<PackageUsage>
{
    public void Configure(EntityTypeBuilder<PackageUsage> builder)
    {
        builder.ToTable("package_usage");

        builder.HasKey(pu => pu.Id);

        builder.Property(pu => pu.Id)
            .ValueGeneratedNever();

        builder.Property(pu => pu.SessionPackageId)
            .IsRequired();

        builder.Property(pu => pu.SessionId)
            .IsRequired();

        builder.Property(pu => pu.UsedAt)
            .IsRequired();

        builder.Property(pu => pu.UsageType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(pu => pu.SessionPackageId);
    }
}
