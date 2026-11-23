using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Errors;

namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>Limits a plan document must respect (shared by plans and templates).</summary>
public static class PlanDocumentRules
{
    public const int MaxWeeks = 52;
    public const int MaxDaysPerWeek = 7;
    public const int MaxExercisesPerDay = 30;
    public const int MaxSets = 20;
    public const int MaxSeconds = 3600;
    public const int MaxRestSeconds = 900;
    public const int MaxNameLength = 200;
    public const int MaxNotesLength = 500;
    public const int MaxRepsLength = 20;
    public const int MaxUrlLength = 500;
    public const decimal MaxWeightKg = 500m;

    /// <summary>Each rule returns the problem with one exercise, or null when it is fine. Checked in order.</summary>
    private static readonly Func<PlanExercise, string?>[] ExerciseRules =
    {
        e => IsBlankOrLong(e.Name, MaxNameLength) ? "Every exercise needs a name." : null,
        e => e.Sets is < 1 or > MaxSets ? $"\"{e.Name}\": sets must be 1 to {MaxSets}." : null,
        e => HasReps(e) == e.Seconds.HasValue ? $"\"{e.Name}\": give either reps or a time." : null,
        e => e.Reps is { Length: > MaxRepsLength } ? $"\"{e.Name}\": reps is too long." : null,
        e => e.Seconds is < 1 or > MaxSeconds ? $"\"{e.Name}\": time must be 1 to {MaxSeconds} seconds." : null,
        e => e.RestSeconds is < 0 or > MaxRestSeconds ? $"\"{e.Name}\": rest must be 0 to {MaxRestSeconds} seconds." : null,
        e => e.TargetWeightKg is < 0 or > MaxWeightKg ? $"\"{e.Name}\": target weight must be 0 to {MaxWeightKg} kg." : null,
        e => e.Rpe is < 1 or > 10 ? $"\"{e.Name}\": RPE must be 1 to 10." : null,
        e => e.Notes is { Length: > MaxNotesLength } ? $"\"{e.Name}\": notes are too long." : null,
        e => IsWebAddressOrEmpty(e.VideoUrl) ? null : $"\"{e.Name}\": the video link must be a web address.",
    };

    public static Result Validate(PlanDocument document)
    {
        var error = DocumentError(document) ?? document.Sequence().Select(DayError).FirstOrDefault(e => e is not null);
        return error is null ? Result.Success() : Result.Failure(TrainingPlanErrors.InvalidDocument(error));
    }

    private static string? DocumentError(PlanDocument document)
    {
        if (document.Weeks.Count is < 1 or > MaxWeeks)
            return $"A plan has 1 to {MaxWeeks} weeks.";
        return document.Weeks.Any(w => w.Days.Count is < 1 or > MaxDaysPerWeek)
            ? $"Every week has 1 to {MaxDaysPerWeek} workout days."
            : null;
    }

    private static string? DayError(PlanDay day)
    {
        if (IsBlankOrLong(day.Title, MaxNameLength))
            return "Every workout day needs a name.";
        if (day.Exercises.Count is < 1 or > MaxExercisesPerDay)
            return $"\"{day.Title}\" needs 1 to {MaxExercisesPerDay} exercises.";
        return day.Exercises.Select(ExerciseError).FirstOrDefault(e => e is not null);
    }

    private static string? ExerciseError(PlanExercise exercise) =>
        ExerciseRules.Select(rule => rule(exercise)).FirstOrDefault(e => e is not null);

    private static bool HasReps(PlanExercise e) => !string.IsNullOrWhiteSpace(e.Reps);

    private static bool IsBlankOrLong(string? value, int max) => string.IsNullOrWhiteSpace(value) || value.Length > max;

    private static bool IsWebAddressOrEmpty(string? url) =>
        url is null
        || (url.Length <= MaxUrlLength
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));
}
