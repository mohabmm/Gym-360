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
        builder.Property(s => s.ChallengeTarget).HasDefaultValue(TrainingPlanSettings.DefaultChallengeTarget);
        builder.Property(s => s.ChallengeWindowDays).HasDefaultValue(TrainingPlanSettings.DefaultChallengeWindowDays);
        builder.Property(s => s.RewardTextAr).HasMaxLength(ChallengeAward.MaxRewardTextLength);
        builder.Property(s => s.RewardTextEn).HasMaxLength(ChallengeAward.MaxRewardTextLength);
        builder.Ignore(s => s.EffectiveRewardTextAr);
        builder.Ignore(s => s.EffectiveRewardTextEn);
    }
}
