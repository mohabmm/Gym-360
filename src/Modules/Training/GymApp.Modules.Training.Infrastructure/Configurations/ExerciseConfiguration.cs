using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

internal sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("exercises");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.NameAr)
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.Instructions)
            .HasMaxLength(4000);

        builder.Property(e => e.DemoMediaUrl)
            .HasMaxLength(500);

        builder.Property(e => e.Equipment)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Scope)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.MuscleGroups)
            .HasConversion(
                v => string.Join(',', v.Select(mg => mg.ToString())),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => Enum.Parse<MuscleGroup>(s))
                    .ToList())
            .HasColumnName("muscle_groups")
            .HasMaxLength(500)
            .Metadata.SetValueComparer(
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<MuscleGroup>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.Scope);
        builder.HasIndex(e => e.GymId);
        builder.HasIndex(e => new { e.Name, e.GymId });

        builder.HasData(ExerciseSeedData.GetExercises());
    }
}
