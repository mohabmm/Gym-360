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

    public static Result Validate(PlanDocument document)
    {
        if (document.Weeks.Count is < 1 or > MaxWeeks)
            return Fail($"A plan has 1 to {MaxWeeks} weeks.");
        if (document.Weeks.Any(w => w.Days.Count is < 1 or > MaxDaysPerWeek))
            return Fail($"Every week has 1 to {MaxDaysPerWeek} workout days.");
        foreach (var day in document.Sequence())
        {
            var error = DayError(day);
            if (error is not null)
                return Fail(error);
        }
        return Result.Success();
    }

    private static string? DayError(PlanDay day)
    {
        if (string.IsNullOrWhiteSpace(day.Title) || day.Title.Length > MaxNameLength)
            return "Every workout day needs a name.";
        if (day.Exercises.Count is < 1 or > MaxExercisesPerDay)
            return $"\"{day.Title}\" needs 1 to {MaxExercisesPerDay} exercises.";
        return day.Exercises.Select(ExerciseError).FirstOrDefault(e => e is not null);
    }

    private static string? ExerciseError(PlanExercise e)
    {
        if (string.IsNullOrWhiteSpace(e.Name) || e.Name.Length > MaxNameLength)
            return "Every exercise needs a name.";
        if (e.Sets is < 1 or > MaxSets)
            return $"\"{e.Name}\": sets must be 1 to {MaxSets}.";
        return MeasureError(e) ?? TargetError(e) ?? TextError(e);
    }

    private static string? MeasureError(PlanExercise e)
    {
        var hasReps = !string.IsNullOrWhiteSpace(e.Reps);
        if (hasReps == e.Seconds.HasValue)
            return $"\"{e.Name}\": give either reps or a time.";
        if (hasReps && e.Reps!.Length > MaxRepsLength)
            return $"\"{e.Name}\": reps is too long.";
        if (e.Seconds is < 1 or > MaxSeconds)
            return $"\"{e.Name}\": time must be 1 to {MaxSeconds} seconds.";
        return e.RestSeconds is < 0 or > MaxRestSeconds ? $"\"{e.Name}\": rest must be 0 to {MaxRestSeconds} seconds." : null;
    }

    private static string? TargetError(PlanExercise e)
    {
        if (e.TargetWeightKg is < 0 or > MaxWeightKg)
            return $"\"{e.Name}\": target weight must be 0 to {MaxWeightKg} kg.";
        return e.Rpe is < 1 or > 10 ? $"\"{e.Name}\": RPE must be 1 to 10." : null;
    }

    private static string? TextError(PlanExercise e)
    {
        if (e.Notes is { Length: > MaxNotesLength })
            return $"\"{e.Name}\": notes are too long.";
        if (e.VideoUrl is null)
            return null;
        var valid = e.VideoUrl.Length <= MaxUrlLength
            && Uri.TryCreate(e.VideoUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        return valid ? null : $"\"{e.Name}\": the video link must be a web address.";
    }

    private static Result Fail(string reason) => Result.Failure(TrainingPlanErrors.InvalidDocument(reason));
}
