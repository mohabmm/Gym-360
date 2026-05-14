using GymApp.Modules.Scheduling.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class TrainerLogConfiguration : IEntityTypeConfiguration<TrainerLog>
{
    public void Configure(EntityTypeBuilder<TrainerLog> builder)
    {
        builder.ToTable("session_trainer_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.SessionId)
            .IsRequired();

        builder.Property(e => e.ExercisesPerformed)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(e => e.ActualVsPlanned)
            .HasMaxLength(2000);

        builder.Property(e => e.ClientEffortRating)
            .IsRequired();

        builder.Property(e => e.Feedback)
            .HasMaxLength(2000);

        builder.Property(e => e.TechniqueNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.AreasToImprove)
            .HasMaxLength(2000);

        builder.Property(e => e.NextSessionFocus)
            .HasMaxLength(500);

        builder.Property(e => e.AttendanceConfirmed)
            .IsRequired();

        builder.Property(e => e.LoggedAt)
            .IsRequired();

        builder.HasIndex(e => e.SessionId)
            .IsUnique()
            .HasDatabaseName("IX_session_trainer_logs_session");
    }
}
