using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Progress.Infrastructure.Persistence.Configurations;

internal sealed class StreakConfiguration : IEntityTypeConfiguration<Streak>
{
    public void Configure(EntityTypeBuilder<Streak> builder)
    {
        builder.ToTable("streaks");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.GymId)
            .IsRequired();

        builder.Property(e => e.CurrentStreak)
            .IsRequired();

        builder.Property(e => e.LongestStreak)
            .IsRequired();

        builder.Property(e => e.StreakType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.LastActivityDate)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.HasIndex(e => new { e.UserId, e.GymId, e.StreakType })
            .IsUnique()
            .HasDatabaseName("IX_streaks_user_gym_type");
    }
}
