using System.Text.Json;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class SessionExerciseLogConfiguration : IEntityTypeConfiguration<SessionExerciseLog>
{
    public void Configure(EntityTypeBuilder<SessionExerciseLog> builder)
    {
        builder.ToTable("session_exercise_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.SessionId).IsRequired();

        builder.Property(e => e.ExerciseId);

        builder.Property(e => e.ExerciseName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ExerciseNameAr)
            .HasMaxLength(200);

        builder.Property(e => e.OrderIndex).IsRequired();

        builder.Property(e => e.Sets).IsRequired();

        builder.Property(e => e.Reps)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Weight)
            .HasColumnType("decimal(8,2)");

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.Property(e => e.TargetReps)
            .HasMaxLength(50);

        builder.Property(e => e.TargetWeight)
            .HasColumnType("decimal(8,2)");

        builder.Property(e => e.TargetSeconds);

        builder.Property(e => e.Measure)
            .HasConversion<int>()
            .HasDefaultValue(ExerciseMeasure.Reps)
            .IsRequired();

        // Per-set detail as a JSON array, mapped through the nullable field: EF never runs a converter
        // on NULL, so logs written before per-set logging keep a null field and read as no sets.
        builder.Property<IReadOnlyList<ExerciseSetEntry>?>("_setEntries")
            .HasColumnName(nameof(SessionExerciseLog.SetEntries))
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => JsonSerializer.Serialize(v, SetEntriesJson.Options),
                v => SetEntriesJson.Read(v),
                SetEntriesJson.Comparer)
            .IsRequired(false);

        builder.Ignore(e => e.SetEntries);

        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.SessionId, e.OrderIndex })
            .HasDatabaseName("IX_session_exercise_logs_session_order");
    }
}

/// <summary>JSON shape of <see cref="SessionExerciseLog.SetEntries"/>: <c>[{"reps":12,"weight":60,"seconds":null}]</c>.</summary>
internal static class SetEntriesJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static readonly ValueComparer<IReadOnlyList<ExerciseSetEntry>?> Comparer = new(
        (a, b) => a == null ? b == null : b != null && a.SequenceEqual(b),
        v => v == null ? 0 : v.Aggregate(0, (hash, set) => HashCode.Combine(hash, set)),
        v => v == null ? null : v.ToList());

    public static IReadOnlyList<ExerciseSetEntry>? Read(string json) =>
        JsonSerializer.Deserialize<List<ExerciseSetEntry>>(json, Options);
}
