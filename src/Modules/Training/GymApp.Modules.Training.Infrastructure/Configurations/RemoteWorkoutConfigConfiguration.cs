using GymApp.Modules.Training.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

internal sealed class RemoteWorkoutConfigConfiguration : IEntityTypeConfiguration<RemoteWorkoutConfig>
{
    public void Configure(EntityTypeBuilder<RemoteWorkoutConfig> builder)
    {
        builder.ToTable("remote_workout_configs");

        builder.HasKey(rwc => rwc.Id);

        builder.Property(rwc => rwc.Id)
            .ValueGeneratedNever();

        builder.Property(rwc => rwc.Reason)
            .HasMaxLength(500);

        builder.HasIndex(rwc => new { rwc.TrainerId, rwc.ClientId, rwc.GymId }).IsUnique();
        builder.HasIndex(rwc => new { rwc.ClientId, rwc.GymId, rwc.IsActive });
    }
}
