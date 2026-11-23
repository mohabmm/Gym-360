using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Training.Plans;

internal static class PlanSamples
{
    public static PlanExercise Exercise(string name, int sets = 3, string? reps = "10", int? seconds = null) =>
        new() { Name = name, Sets = sets, Reps = reps, Seconds = seconds, RestSeconds = 60 };

    public static PlanDay Day(string title, params PlanExercise[] exercises) =>
        new() { Title = title, Exercises = exercises };

    public static PlanDocument OneWeek(params PlanDay[] days) => PlanDocument.Of(new[] { new PlanWeek { Week = 1, Days = days } });

    public static PlanDocument FullBodyWeek() => OneWeek(
        Day("Day A", Exercise("Squat"), Exercise("Bench press")),
        Day("Day B", Exercise("Deadlift", 3, "5"), Exercise("Plank", 3, reps: null, seconds: 45)),
        Day("Day C", Exercise("Lunge"), Exercise("Row")));

    public static PlanContent Content(PlanDocument? document = null, string name = "Beginner full body") =>
        Validate(name, PlanGoal.GeneralFitness, null, document ?? FullBodyWeek()).Value with { NameAr = "فول بادي للمبتدئين" };

    public static GymApp.Common.Domain.Result<PlanContent> Validate(string name, PlanGoal goal, string? goalText, PlanDocument document) =>
        PlanContent.Create(new PlanContent { Name = name, Goal = goal, GoalText = goalText, Document = document });
}
