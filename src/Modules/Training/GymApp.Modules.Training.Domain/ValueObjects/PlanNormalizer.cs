namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>
/// Puts a plan document in canonical form: weeks numbered 1..N, text trimmed, and a unique key on
/// every day and exercise. Existing keys are kept, so completions recorded against a day still
/// match it after the coach edits the plan.
/// </summary>
internal sealed class PlanNormalizer
{
    private const int GeneratedKeyLength = 8;
    private readonly HashSet<string> _used = new(StringComparer.Ordinal);

    public static PlanDocument Normalize(PlanDocument document)
    {
        var normalizer = new PlanNormalizer();
        var weeks = document.Weeks
            .OrderBy(w => w.Week)
            .Select((w, index) => new PlanWeek { Week = index + 1, Days = w.Days.Select(normalizer.Day).ToList() })
            .ToList();
        return new PlanDocument { Weeks = weeks };
    }

    /// <summary>A copy of the week whose days and exercises will get fresh keys.</summary>
    public static PlanWeek WithoutKeys(PlanWeek week) => week with
    {
        Days = week.Days.Select(d => d with
        {
            Key = string.Empty,
            Exercises = d.Exercises.Select(e => e with { Key = string.Empty }).ToList()
        }).ToList()
    };

    private PlanDay Day(PlanDay day) => day with
    {
        Key = Take(day.Key, "d-"),
        Title = day.Title.Trim(),
        TitleAr = day.TitleAr.OrNull(),
        Exercises = day.Exercises.Select(Exercise).ToList()
    };

    private PlanExercise Exercise(PlanExercise exercise) => exercise with
    {
        Key = Take(exercise.Key, "e-"),
        Name = exercise.Name.Trim(),
        NameAr = exercise.NameAr.OrNull(),
        Reps = exercise.Reps.OrNull(),
        Notes = exercise.Notes.OrNull(),
        VideoUrl = exercise.VideoUrl.OrNull()
    };

    private string Take(string? key, string prefix)
    {
        var candidate = key.OrNull();
        while (candidate is null || !_used.Add(candidate))
            candidate = prefix + Guid.NewGuid().ToString("N")[..GeneratedKeyLength];
        return candidate;
    }
}

public static class PlanText
{
    /// <summary>Trimmed text, or null when blank.</summary>
    public static string? OrNull(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
