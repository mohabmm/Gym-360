using GymApp.Modules.Training.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

internal sealed class WorkoutExerciseConfiguration : IEntityTypeConfiguration<WorkoutExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutExercise> builder)
    {
        builder.ToTable("workout_exercises");

        builder.HasKey(we => we.Id);

        builder.Property(we => we.Id)
            .ValueGeneratedNever();

        builder.Property(we => we.Reps)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(we => we.Weight)
            .HasPrecision(10, 2);

        builder.Property(we => we.Notes)
            .HasMaxLength(500);

        builder.HasIndex(we => new { we.WorkoutId, we.Order });
    }
}
