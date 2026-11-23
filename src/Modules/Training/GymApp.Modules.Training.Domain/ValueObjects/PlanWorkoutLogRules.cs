using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Errors;

namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>Limits a finished plan workout must respect.</summary>
public static class PlanWorkoutLogRules
{
    public const int MaxSets = PlanDocumentRules.MaxExercisesPerDay * PlanDocumentRules.MaxSets;
    public const int MaxReps = 500;
    public const int MaxDurationMinutes = 600;

    private static readonly Func<CompletedSet, string?>[] SetRules =
    {
        s => string.IsNullOrWhiteSpace(s.ExerciseKey) || string.IsNullOrWhiteSpace(s.ExerciseName) ? "Every set needs its exercise." : null,
        s => s.ExerciseName.Length > PlanDocumentRules.MaxNameLength || s.ExerciseKey.Length > 50 ? "An exercise name is too long." : null,
        s => s.SetNumber is < 1 or > PlanDocumentRules.MaxSets ? $"Set numbers go from 1 to {PlanDocumentRules.MaxSets}." : null,
        s => s.Reps is < 0 or > MaxReps ? $"Reps must be 0 to {MaxReps}." : null,
        s => s.WeightKg is < 0 or > PlanDocumentRules.MaxWeightKg ? $"Weight must be 0 to {PlanDocumentRules.MaxWeightKg} kg." : null,
        s => s.Seconds is < 0 or > PlanDocumentRules.MaxSeconds ? $"Time must be 0 to {PlanDocumentRules.MaxSeconds} seconds." : null,
    };

    public static Result Validate(PlanWorkoutLog log)
    {
        var error = LogError(log) ?? log.Sets.SelectMany(s => SetRules.Select(rule => rule(s))).FirstOrDefault(e => e is not null);
        return error is null ? Result.Success() : Result.Failure(TrainingPlanErrors.InvalidWorkout(error));
    }

    private static string? LogError(PlanWorkoutLog log)
    {
        if (log.ClientId == Guid.Empty)
            return "The workout needs an id.";
        if (log.Sets.Count > MaxSets)
            return $"A workout has at most {MaxSets} sets.";
        if (log.DurationMinutes is < 0 or > MaxDurationMinutes)
            return $"Duration must be 0 to {MaxDurationMinutes} minutes.";
        return log.Note is { Length: > PlanDocumentRules.MaxNotesLength } ? "The note is too long." : null;
    }
}
