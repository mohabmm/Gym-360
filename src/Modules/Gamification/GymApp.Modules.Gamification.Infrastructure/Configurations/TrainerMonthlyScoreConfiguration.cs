using GymApp.Modules.Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gamification.Infrastructure.Configurations;

internal sealed class TrainerMonthlyScoreConfiguration : IEntityTypeConfiguration<TrainerMonthlyScore>
{
    public void Configure(EntityTypeBuilder<TrainerMonthlyScore> builder)
    {
        builder.ToTable("trainer_monthly_scores");

        builder.HasKey(tms => tms.Id);

        builder.Property(tms => tms.Id)
            .ValueGeneratedNever();

        builder.Property(tms => tms.GymId)
            .IsRequired();

        builder.Property(tms => tms.TrainerId)
            .IsRequired();

        builder.Property(tms => tms.Month)
            .IsRequired();

        builder.Property(tms => tms.Year)
            .IsRequired();

        builder.Property(tms => tms.AggregatedScore)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(tms => tms.TotalRatings)
            .IsRequired();

        builder.Property(tms => tms.UniqueClients)
            .IsRequired();

        builder.Property(tms => tms.SessionCount)
            .IsRequired();

        builder.Property(tms => tms.NormalizedScore)
            .HasColumnType("decimal(10,4)")
            .IsRequired();

        builder.Property(tms => tms.IsCoachOfTheMonth)
            .IsRequired();

        builder.Property(tms => tms.CalculatedAt)
            .IsRequired();

        builder.HasIndex(tms => new { tms.GymId, tms.TrainerId, tms.Month, tms.Year })
            .IsUnique();
    }
}
