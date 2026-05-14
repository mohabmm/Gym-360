using GymApp.Modules.Gamification.Domain.Entities;
using GymApp.Modules.Gamification.Domain.Enums;
using GymApp.Modules.Gamification.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gamification.Infrastructure.Configurations;

internal sealed class MemberLeaderboardConfiguration : IEntityTypeConfiguration<MemberLeaderboard>
{
    public void Configure(EntityTypeBuilder<MemberLeaderboard> builder)
    {
        builder.ToTable("member_leaderboards");

        builder.HasKey(ml => ml.Id);

        builder.Property(ml => ml.Id)
            .ValueGeneratedNever();

        builder.Property(ml => ml.GymId)
            .IsRequired();

        builder.OwnsOne(ml => ml.Period, periodBuilder =>
        {
            periodBuilder.Property(p => p.Month)
                .HasColumnName("period_month")
                .IsRequired();

            periodBuilder.Property(p => p.Year)
                .HasColumnName("period_year")
                .IsRequired();
        });

        builder.Property(ml => ml.LeaderboardType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(ml => ml.RecalculatedAt)
            .IsRequired();

        builder.Property(ml => ml.CreatedAt)
            .IsRequired();

        builder.HasMany(ml => ml.Entries)
            .WithOne()
            .HasForeignKey(e => e.LeaderboardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ml => ml.GymId);
    }
}
