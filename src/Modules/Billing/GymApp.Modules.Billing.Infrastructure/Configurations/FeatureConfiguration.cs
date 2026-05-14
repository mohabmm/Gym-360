using GymApp.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Billing.Infrastructure.Configurations;

internal sealed class FeatureConfiguration : IEntityTypeConfiguration<Feature>
{
    public void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.ToTable("features");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .ValueGeneratedNever();

        builder.Property(f => f.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(f => f.Code)
            .IsUnique();

        builder.Property(f => f.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.Description)
            .HasMaxLength(500);

        builder.Property(f => f.Category)
            .HasMaxLength(50);

        builder.Property(f => f.IsActive)
            .IsRequired();

        builder.Property(f => f.SortOrder)
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .IsRequired();
    }
}
