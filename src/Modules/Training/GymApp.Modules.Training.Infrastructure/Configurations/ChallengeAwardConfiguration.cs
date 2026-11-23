using GymApp.Modules.Training.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

internal sealed class ChallengeAwardConfiguration : IEntityTypeConfiguration<ChallengeAward>
{
    public void Configure(EntityTypeBuilder<ChallengeAward> builder)
    {
        builder.ToTable("challenge_awards");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        // One award per member per gym: this is what makes the announcement fire once.
        builder.HasIndex(a => new { a.GymId, a.TraineeId }).IsUnique();
        builder.HasIndex(a => new { a.GymId, a.DeliveredAt });
        builder.Property(a => a.RewardTextAr).HasMaxLength(ChallengeAward.MaxRewardTextLength).IsRequired();
        builder.Property(a => a.RewardTextEn).HasMaxLength(ChallengeAward.MaxRewardTextLength).IsRequired();
        builder.Ignore(a => a.IsDelivered);
    }
}
