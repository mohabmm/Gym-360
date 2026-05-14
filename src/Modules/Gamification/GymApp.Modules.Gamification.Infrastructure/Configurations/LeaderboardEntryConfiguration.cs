using GymApp.Modules.Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gamification.Infrastructure.Configurations;

internal sealed class LeaderboardEntryConfiguration : IEntityTypeConfiguration<LeaderboardEntry>
{
    public void Configure(EntityTypeBuilder<LeaderboardEntry> builder)
    {
        builder.ToTable("leaderboard_entries");

        builder.HasKey(le => le.Id);

        builder.Property(le => le.Id)
            .ValueGeneratedNever();

        builder.Property(le => le.LeaderboardId)
            .IsRequired();

        builder.Property(le => le.MemberId)
            .IsRequired();

        builder.Property(le => le.Rank)
            .IsRequired();

        builder.Property(le => le.Score)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(le => le.Delta)
            .HasColumnType("decimal(10,2)");

        builder.Property(le => le.IsOptedIn)
            .IsRequired();

        builder.HasIndex(le => le.LeaderboardId);
        builder.HasIndex(le => le.MemberId);
    }
}
