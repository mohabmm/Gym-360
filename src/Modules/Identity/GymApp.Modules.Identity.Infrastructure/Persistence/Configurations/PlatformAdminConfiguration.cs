using GymApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class PlatformAdminConfiguration : IEntityTypeConfiguration<PlatformAdmin>
{
    public void Configure(EntityTypeBuilder<PlatformAdmin> builder)
    {
        builder.ToTable("platform_admins");

        builder.HasKey(pa => pa.Id);

        builder.Property(pa => pa.Id)
            .ValueGeneratedNever();

        builder.Property(pa => pa.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasIndex(pa => pa.UserId)
            .IsUnique();

        builder.Property(pa => pa.AdminRole)
            .HasColumnName("admin_role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(pa => pa.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(pa => pa.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
