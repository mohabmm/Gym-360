using GymApp.Modules.Training.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

internal sealed class TrainingPlanSettingsConfiguration : IEntityTypeConfiguration<TrainingPlanSettings>
{
    public void Configure(EntityTypeBuilder<TrainingPlanSettings> builder)
    {
        builder.ToTable("training_plan_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.HasIndex(s => s.GymId).IsUnique();
        builder.Property(s => s.InactiveDays).HasDefaultValue(TrainingPlanSettings.DefaultInactiveDays);
    }
}
