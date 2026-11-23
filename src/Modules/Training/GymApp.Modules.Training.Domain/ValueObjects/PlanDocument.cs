namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>
/// pt.training_plan: the body of a plan or a template, stored as one versioned JSON document
/// (plan schema v<see cref="CurrentSchemaVersion"/>) so a plan can later be drafted as JSON.
/// Immutable: every change returns a new document, so a plan copied from a template never shares
/// state with it.
/// </summary>
public sealed record PlanDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required IReadOnlyList<PlanWeek> Weeks { get; init; }

    public static PlanDocument Of(IEnumerable<PlanWeek> weeks) => new() { Weeks = weeks.ToList() };

    public int DurationWeeks => Weeks.Count;

    /// <summary>Every workout day in the order the trainee does them: week 1 day 1, week 1 day 2, … .</summary>
    public IReadOnlyList<PlanDay> Sequence() => Weeks.OrderBy(w => w.Week).SelectMany(w => w.Days).ToList();

    /// <summary>Repeats week 1 for <paramref name="durationWeeks"/> weeks (fresh keys for each copy).</summary>
    public PlanDocument CopyFirstWeekToAll(int durationWeeks)
    {
        var first = Weeks.OrderBy(w => w.Week).FirstOrDefault();
        if (first is null || durationWeeks < 1)
            return this;
        var copies = Enumerable.Range(2, durationWeeks - 1).Select(_ => PlanNormalizer.WithoutKeys(first));
        return Of(copies.Prepend(first)).Normalized();
    }

    /// <summary>Numbers weeks 1..N in order and gives every day and exercise a unique, stable key.</summary>
    public PlanDocument Normalized() => PlanNormalizer.Normalize(this);
}

public sealed record PlanWeek
{
    public required int Week { get; init; }
    public required IReadOnlyList<PlanDay> Days { get; init; }
}

/// <summary>One workout day, e.g. "Day A – Upper body". <see cref="Key"/> stays the same across edits.</summary>
public sealed record PlanDay
{
    public string Key { get; init; } = string.Empty;
    public required string Title { get; init; }
    public string? TitleAr { get; init; }
    public required IReadOnlyList<PlanExercise> Exercises { get; init; }
}

/// <summary>
/// One exercise of a day: from the library (<see cref="ExerciseId"/>) or free text (no id).
/// Either <see cref="Reps"/> (e.g. "8-10") or <see cref="Seconds"/> (timed exercise).
/// </summary>
public sealed record PlanExercise
{
    public string Key { get; init; } = string.Empty;
    public Guid? ExerciseId { get; init; }
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public required int Sets { get; init; }
    public string? Reps { get; init; }
    public int? Seconds { get; init; }
    public int? RestSeconds { get; init; }
    public decimal? TargetWeightKg { get; init; }
    public decimal? Rpe { get; init; }
    public string? Notes { get; init; }
    public string? VideoUrl { get; init; }
}
