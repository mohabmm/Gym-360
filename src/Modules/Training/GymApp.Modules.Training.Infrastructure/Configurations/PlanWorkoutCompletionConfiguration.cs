using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

internal sealed class PlanWorkoutCompletionConfiguration : IEntityTypeConfiguration<PlanWorkoutCompletion>
{
    public void Configure(EntityTypeBuilder<PlanWorkoutCompletion> builder)
    {
        builder.ToTable("plan_workout_completions");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.DayKey).HasMaxLength(50).IsRequired();
        builder.Property(c => c.DayTitle).HasMaxLength(PlanDocumentRules.MaxNameLength).IsRequired();
        builder.Property(c => c.DayTitleAr).HasMaxLength(PlanDocumentRules.MaxNameLength);
        builder.Property(c => c.Note).HasMaxLength(PlanDocumentRules.MaxNotesLength);
        builder.Property(c => c.Source).HasConversion<string>().HasMaxLength(20);
        builder.MapSets(nameof(PlanWorkoutCompletion.Sets));
        builder.Ignore(c => c.Mark);
        builder.Ignore(c => c.SetsDone);

        // A retried upload from the device finds the first one instead of counting twice.
        builder.HasIndex(c => new { c.TraineeId, c.ClientId })
            .IsUnique()
            .HasDatabaseName("UX_plan_workout_completions_client");
        builder.HasIndex(c => new { c.GymId, c.PlanId });
        builder.HasIndex(c => new { c.GymId, c.TraineeId, c.CompletedOn });
        builder.HasIndex(c => new { c.GymId, c.SessionId }).HasFilter("[SessionId] IS NOT NULL");
    }
}
