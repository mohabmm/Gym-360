using GymApp.Modules.Gyms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gyms.Infrastructure.Data.Configurations;

internal sealed class GymMemberConfiguration : IEntityTypeConfiguration<GymMember>
{
    public void Configure(EntityTypeBuilder<GymMember> builder)
    {
        builder.ToTable("gym_members");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.GymId).IsRequired();
        builder.Property(m => m.MemberId).IsRequired();

        builder.Property(m => m.MemberNumber)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(m => m.Section).HasMaxLength(60);
        builder.Property(m => m.JoinedAt).IsRequired();

        builder.Property(m => m.IsFrozen).IsRequired().HasDefaultValue(false);
        builder.Property(m => m.FrozenAt);

        builder.HasIndex(m => new { m.GymId, m.MemberId }).IsUnique();
        builder.HasIndex(m => new { m.GymId, m.MemberNumber }).IsUnique();
    }
}
