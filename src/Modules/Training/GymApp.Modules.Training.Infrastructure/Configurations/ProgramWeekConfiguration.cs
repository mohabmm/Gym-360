using GymApp.Modules.Training.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

internal sealed class ProgramWeekConfiguration : IEntityTypeConfiguration<ProgramWeek>
{
    public void Configure(EntityTypeBuilder<ProgramWeek> builder)
    {
        builder.ToTable("program_weeks");

        builder.HasKey(pw => pw.Id);

        builder.Property(pw => pw.Id)
            .ValueGeneratedNever();

        builder.Property(pw => pw.Theme)
            .HasMaxLength(200);

        builder.Property(pw => pw.DayWorkouts)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<ProgramDayWorkout>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<ProgramDayWorkout>())
            .HasColumnName("day_workouts")
            .HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<ProgramDayWorkout>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        builder.HasIndex(pw => new { pw.ProgramId, pw.WeekNumber }).IsUnique();
    }
}
