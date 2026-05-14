using GymApp.Modules.Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Gamification.Infrastructure.Configurations;

internal sealed class LeaderboardOptOutConfiguration : IEntityTypeConfiguration<LeaderboardOptOut>
{
    public void Configure(EntityTypeBuilder<LeaderboardOptOut> builder)
    {
        builder.ToTable("leaderboard_opt_outs");

        builder.HasKey(lo => lo.Id);

        builder.Property(lo => lo.Id)
            .ValueGeneratedNever();

        builder.Property(lo => lo.UserId)
            .IsRequired();

        builder.Property(lo => lo.GymId)
            .IsRequired();

        builder.Property(lo => lo.ShowOnLeaderboard)
            .IsRequired();

        builder.Property(lo => lo.UpdatedAt)
            .IsRequired();

        builder.HasIndex(lo => new { lo.UserId, lo.GymId })
            .IsUnique();
    }
}
