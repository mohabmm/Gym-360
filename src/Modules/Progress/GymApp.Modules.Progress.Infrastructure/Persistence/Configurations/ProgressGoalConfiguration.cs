using GymApp.Modules.Progress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Mirrors <c>ProgressGoalsSchemaPatch</c>, which adds the table to existing per-gym schemas.</summary>
internal sealed class ProgressGoalConfiguration : IEntityTypeConfiguration<ProgressGoal>
{
    public void Configure(EntityTypeBuilder<ProgressGoal> builder)
    {
        builder.ToTable("progress_goals");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.GymId).IsRequired();

        builder.Property(e => e.Goal)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.TargetWeight).HasPrecision(8, 2);
        builder.Property(e => e.SetBy).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => new { e.UserId, e.GymId })
            .IsUnique()
            .HasDatabaseName("IX_progress_goals_user_gym");
    }
}
