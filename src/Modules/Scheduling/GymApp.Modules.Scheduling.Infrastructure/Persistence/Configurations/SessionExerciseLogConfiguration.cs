using GymApp.Modules.Scheduling.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class SessionExerciseLogConfiguration : IEntityTypeConfiguration<SessionExerciseLog>
{
    public void Configure(EntityTypeBuilder<SessionExerciseLog> builder)
    {
        builder.ToTable("session_exercise_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.SessionId).IsRequired();

        builder.Property(e => e.ExerciseId);

        builder.Property(e => e.ExerciseName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ExerciseNameAr)
            .HasMaxLength(200);

        builder.Property(e => e.OrderIndex).IsRequired();

        builder.Property(e => e.Sets).IsRequired();

        builder.Property(e => e.Reps)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Weight)
            .HasColumnType("decimal(8,2)");

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.SessionId, e.OrderIndex })
            .HasDatabaseName("IX_session_exercise_logs_session_order");
    }
}
