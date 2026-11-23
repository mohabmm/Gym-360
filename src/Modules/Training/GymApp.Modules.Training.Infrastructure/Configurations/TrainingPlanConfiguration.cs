using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

internal sealed class TrainingPlanConfiguration : IEntityTypeConfiguration<TrainingPlan>
{
    public void Configure(EntityTypeBuilder<TrainingPlan> builder)
    {
        builder.ToTable("training_plans");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Goal).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.Name).HasMaxLength(PlanDocumentRules.MaxNameLength).IsRequired();
        builder.Property(p => p.NameAr).HasMaxLength(PlanDocumentRules.MaxNameLength);
        builder.Property(p => p.GoalText).HasMaxLength(PlanDocumentRules.MaxNameLength);
        builder.Property(p => p.Version).IsConcurrencyToken();
        builder.MapDocument(nameof(TrainingPlan.Document));
        builder.Ignore(p => p.Content);
        builder.Ignore(p => p.IsActive);

        // One active plan per trainee: the database enforces it even under concurrent assigns.
        builder.HasIndex(p => new { p.GymId, p.TraineeId })
            .IsUnique()
            .HasFilter("[Status] = 'Active'")
            .HasDatabaseName("UX_training_plans_active_trainee");
        builder.HasIndex(p => new { p.GymId, p.TraineeId, p.CreatedAt });
        builder.HasIndex(p => new { p.GymId, p.TrainerId, p.Status });
    }
}
