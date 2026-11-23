using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Training.Plans;

internal static class PlanSamples
{
    public static PlanExercise Exercise(string name, int sets = 3, string? reps = "10", int? seconds = null) =>
        new(Key: string.Empty, ExerciseId: null, name, NameAr: null, sets, reps, seconds,
            RestSeconds: 60, TargetWeightKg: null, Rpe: null, Notes: null, VideoUrl: null);

    public static PlanDay Day(string title, params PlanExercise[] exercises) =>
        new(Key: string.Empty, title, TitleAr: null, exercises);

    public static PlanDocument OneWeek(params PlanDay[] days) => PlanDocument.Of(new[] { new PlanWeek(1, days) });

    public static PlanDocument FullBodyWeek() => OneWeek(
        Day("Day A", Exercise("Squat"), Exercise("Bench press")),
        Day("Day B", Exercise("Deadlift", 3, "5"), Exercise("Plank", 3, reps: null, seconds: 45)),
        Day("Day C", Exercise("Lunge"), Exercise("Row")));

    public static PlanContent Content(PlanDocument? document = null, string name = "Beginner full body") =>
        PlanContent.Create(name, "فول بادي للمبتدئين", PlanGoal.GeneralFitness, null, document ?? FullBodyWeek())
            .Value;
}
