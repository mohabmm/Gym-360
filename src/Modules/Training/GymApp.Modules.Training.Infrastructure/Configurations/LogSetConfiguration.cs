using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

internal sealed class LogSetConfiguration : IEntityTypeConfiguration<LogSet>
{
    public void Configure(EntityTypeBuilder<LogSet> builder)
    {
        builder.ToTable("log_sets");

        builder.HasKey(ls => ls.Id);

        builder.Property(ls => ls.Id)
            .ValueGeneratedNever();

        builder.Property(ls => ls.Weight)
            .HasPrecision(10, 2);

        builder.Property(ls => ls.WeightUnit)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(ls => ls.Notes)
            .HasMaxLength(500);

        builder.HasIndex(ls => new { ls.WorkoutLogId, ls.ExerciseId, ls.SetNumber });
    }
}
