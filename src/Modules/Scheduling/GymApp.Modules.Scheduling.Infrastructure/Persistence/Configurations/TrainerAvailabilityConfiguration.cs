using GymApp.Modules.Scheduling.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class TrainerAvailabilityConfiguration : IEntityTypeConfiguration<TrainerAvailability>
{
    public void Configure(EntityTypeBuilder<TrainerAvailability> builder)
    {
        builder.ToTable("trainer_availabilities");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.TrainerId)
            .IsRequired();

        builder.Property(e => e.GymId)
            .IsRequired();

        builder.Property(e => e.SessionDurationMinutes)
            .IsRequired()
            .HasDefaultValue(60);

        builder.Property(e => e.BufferMinutes)
            .IsRequired()
            .HasDefaultValue(15);

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.HasMany(e => e.WeeklySlots)
            .WithOne()
            .HasForeignKey(s => s.TrainerAvailabilityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.BlockedDates)
            .WithOne()
            .HasForeignKey(b => b.TrainerAvailabilityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TrainerId, e.GymId })
            .IsUnique()
            .HasDatabaseName("IX_trainer_availabilities_trainer_gym");
    }
}
