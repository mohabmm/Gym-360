using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.GymId)
            .IsRequired();

        builder.Property(e => e.TrainerId)
            .IsRequired();

        builder.Property(e => e.MemberId)
            .IsRequired();

        builder.Property(e => e.ScheduledDate)
            .IsRequired();

        builder.Property(e => e.ScheduledTime)
            .IsRequired();

        builder.Property(e => e.DurationMinutes)
            .IsRequired();

        builder.Property(e => e.SessionType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Focus)
            .HasMaxLength(500);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.IsRecurring)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.RecurrenceRule)
            .HasMaxLength(200);

        builder.Property(e => e.CancellationReason)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.TrainerLog)
            .WithOne()
            .HasForeignKey<TrainerLog>(t => t.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.MemberLog)
            .WithOne()
            .HasForeignKey<MemberLog>(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.ExerciseLogs)
            .WithOne()
            .HasForeignKey(l => l.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Session.ExerciseLogs))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => new { e.TrainerId, e.ScheduledDate })
            .HasDatabaseName("IX_sessions_trainer_date");

        builder.HasIndex(e => new { e.MemberId, e.ScheduledDate })
            .HasDatabaseName("IX_sessions_member_date");

        builder.HasIndex(e => new { e.GymId, e.Status })
            .HasDatabaseName("IX_sessions_gym_status");
    }
}
