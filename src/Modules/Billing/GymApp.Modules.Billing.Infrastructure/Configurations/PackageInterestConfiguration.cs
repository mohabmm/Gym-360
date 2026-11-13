using GymApp.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

/// <summary>pt.package_interest leads. Mirrored by PackageInterestsSchemaPatch for existing per-gym schemas.</summary>
internal sealed class PackageInterestConfiguration : IEntityTypeConfiguration<PackageInterest>
{
    public void Configure(EntityTypeBuilder<PackageInterest> builder)
    {
        builder.ToTable("package_interests");

        builder.HasKey(pi => pi.Id);
        builder.Property(pi => pi.Id).ValueGeneratedNever();

        builder.Property(pi => pi.GymId).IsRequired();
        builder.Property(pi => pi.MemberId).IsRequired();
        builder.Property(pi => pi.TrainerId).IsRequired();
        builder.Property(pi => pi.SourcePackageId).IsRequired();

        builder.Property(pi => pi.Source)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(pi => pi.RecommendedSessions);

        builder.Property(pi => pi.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(pi => pi.CreatedAt).IsRequired();
        builder.Property(pi => pi.DismissedAt);
        builder.Property(pi => pi.DismissedBy);

        builder.HasIndex(pi => new { pi.GymId, pi.MemberId });
    }
}
