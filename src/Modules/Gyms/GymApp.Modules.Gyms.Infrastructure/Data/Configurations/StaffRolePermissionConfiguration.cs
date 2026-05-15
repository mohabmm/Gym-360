using GymApp.Modules.Gyms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gyms.Infrastructure.Data.Configurations;

internal sealed class StaffRolePermissionConfiguration : IEntityTypeConfiguration<StaffRolePermission>
{
    public void Configure(EntityTypeBuilder<StaffRolePermission> builder)
    {
        builder.ToTable("staff_role_permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.GymId).IsRequired();

        builder.Property(p => p.StaffRole)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.PermissionCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.IsAllowed).IsRequired();

        builder.HasIndex(p => new { p.GymId, p.StaffRole, p.PermissionCode })
            .IsUnique();
    }
}
