using GymApp.Modules.Gyms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gyms.Infrastructure.Data.Configurations;

internal sealed class GymSessionPolicyConfiguration : IEntityTypeConfiguration<GymSessionPolicy>
{
    public void Configure(EntityTypeBuilder<GymSessionPolicy> builder)
    {
        builder.ToTable("gym_session_policies");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.GymId).IsRequired();

        builder.Property(p => p.CancellationWindowHours).IsRequired();
        builder.Property(p => p.NoshowThreshold).IsRequired();
        builder.Property(p => p.NoshowWindowDays).IsRequired();

        builder.Property(p => p.NoshowAction)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);
        builder.Property(p => p.UpdatedBy);

        builder.HasIndex(p => p.GymId)
            .IsUnique();
    }
}
