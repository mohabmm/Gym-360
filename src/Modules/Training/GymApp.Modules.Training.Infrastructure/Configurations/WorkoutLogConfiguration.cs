using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

internal sealed class WorkoutLogConfiguration : IEntityTypeConfiguration<WorkoutLog>
{
    public void Configure(EntityTypeBuilder<WorkoutLog> builder)
    {
        builder.ToTable("workout_logs");

        builder.HasKey(wl => wl.Id);

        builder.Property(wl => wl.Id)
            .ValueGeneratedNever();

        builder.Property(wl => wl.Notes)
            .HasMaxLength(2000);

        builder.Property(wl => wl.MuscleGroupsWorked)
            .HasConversion(
                v => string.Join(',', v.Select(mg => mg.ToString())),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => Enum.Parse<MuscleGroup>(s))
                    .ToList())
            .HasColumnName("muscle_groups_worked")
            .HasMaxLength(500)
            .Metadata.SetValueComparer(
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<MuscleGroup>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        builder.HasMany(wl => wl.LogSets)
            .WithOne()
            .HasForeignKey(ls => ls.WorkoutLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(wl => new { wl.UserId, wl.Date, wl.WorkoutId }).IsUnique();
        builder.HasIndex(wl => new { wl.UserId, wl.GymId });
    }
}
