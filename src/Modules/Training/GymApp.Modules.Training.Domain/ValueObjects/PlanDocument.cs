namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>
/// pt.training_plan: the body of a plan or a template, stored as one versioned JSON document
/// (plan schema v<see cref="CurrentSchemaVersion"/>) so a plan can later be drafted as JSON.
/// Immutable: every change returns a new document, so a plan copied from a template never shares
/// state with it.
/// </summary>
public sealed record PlanDocument(int SchemaVersion, IReadOnlyList<PlanWeek> Weeks)
{
    public const int CurrentSchemaVersion = 1;

    public static PlanDocument Of(IEnumerable<PlanWeek> weeks) => new(CurrentSchemaVersion, weeks.ToList());

    public int DurationWeeks => Weeks.Count;

    /// <summary>Every workout day in the order the trainee does them: week 1 day 1, week 1 day 2, … .</summary>
    public IReadOnlyList<PlanDay> Sequence() => Weeks.OrderBy(w => w.Week).SelectMany(w => w.Days).ToList();

    public PlanDay? FindDay(string dayKey) => Sequence().FirstOrDefault(d => d.Key == dayKey);

    /// <summary>Repeats week 1 for <paramref name="durationWeeks"/> weeks (fresh keys for each copy).</summary>
    public PlanDocument CopyFirstWeekToAll(int durationWeeks)
    {
        var first = Weeks.OrderBy(w => w.Week).FirstOrDefault();
        if (first is null || durationWeeks < 1)
            return this;
        var weeks = Enumerable.Range(1, durationWeeks)
            .Select(n => n == 1 ? first with { Week = 1 } : first.CopyAs(n))
            .ToList();
        return new PlanDocument(CurrentSchemaVersion, weeks).Normalized();
    }

    /// <summary>Numbers weeks 1..N in order and gives every day and exercise a unique, stable key.</summary>
    public PlanDocument Normalized()
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        var weeks = Weeks
            .OrderBy(w => w.Week)
            .Select((w, i) => w.Normalized(i + 1, used))
            .ToList();
        return new PlanDocument(CurrentSchemaVersion, weeks);
    }

    internal static string UniqueKey(string? key, string prefix, HashSet<string> used)
    {
        var candidate = string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        while (candidate is null || !used.Add(candidate))
            candidate = $"{prefix}{Guid.NewGuid():N}"[..(prefix.Length + 8)];
        return candidate;
    }
}

public sealed record PlanWeek(int Week, IReadOnlyList<PlanDay> Days)
{
    internal PlanWeek CopyAs(int week) =>
        new(week, Days.Select(d => d.Copy()).ToList());

    internal PlanWeek Normalized(int week, HashSet<string> used) =>
        new(week, Days.Select(d => d.Normalized(used)).ToList());
}

/// <summary>One workout day, e.g. "Day A – Upper body". <see cref="Key"/> stays the same across edits.</summary>
public sealed record PlanDay(string Key, string Title, string? TitleAr, IReadOnlyList<PlanExercise> Exercises)
{
    internal PlanDay Copy() =>
        this with { Key = string.Empty, Exercises = Exercises.Select(e => e with { Key = string.Empty }).ToList() };

    internal PlanDay Normalized(HashSet<string> used) => this with
    {
        Key = PlanDocument.UniqueKey(Key, "d-", used),
        Title = Title.Trim(),
        TitleAr = string.IsNullOrWhiteSpace(TitleAr) ? null : TitleAr.Trim(),
        Exercises = Exercises.Select(e => e.Normalized(used)).ToList()
    };
}

/// <summary>
/// One exercise of a day: from the library (<see cref="ExerciseId"/>) or free text (no id).
/// Either <see cref="Reps"/> (e.g. "8-10") or <see cref="Seconds"/> (timed exercise).
/// </summary>
public sealed record PlanExercise(
    string Key,
    Guid? ExerciseId,
    string Name,
    string? NameAr,
    int Sets,
    string? Reps,
    int? Seconds,
    int? RestSeconds,
    decimal? TargetWeightKg,
    decimal? Rpe,
    string? Notes,
    string? VideoUrl)
{
    internal PlanExercise Normalized(HashSet<string> used) => this with
    {
        Key = PlanDocument.UniqueKey(Key, "e-", used),
        Name = Name.Trim(),
        NameAr = Blank(NameAr),
        Reps = Blank(Reps),
        Notes = Blank(Notes),
        VideoUrl = Blank(VideoUrl)
    };

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
