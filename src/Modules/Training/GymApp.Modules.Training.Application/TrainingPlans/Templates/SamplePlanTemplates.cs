using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.TrainingPlans.Templates;

/// <summary>
/// The three editable gym templates every gym starts with (Arabic first). Exercises name the
/// global library in English so they link to it (Arabic name, demo video, progress); a name the
/// library doesn't have stays a free-text exercise with the Arabic name given here.
/// </summary>
internal static class SamplePlanTemplates
{
    internal sealed record Sample(string Key, string NameAr, string NameEn, PlanGoal Goal, IReadOnlyList<SampleDay> Days);

    internal sealed record SampleDay(string TitleAr, string TitleEn, IReadOnlyList<SampleExercise> Exercises);

    /// <param name="Library">English name in the global exercise library.</param>
    /// <param name="NameAr">Arabic name used when the library has none.</param>
    internal sealed record SampleExercise(string Library, string NameAr, int Sets, string? Reps, int? Seconds, int Rest, string? NoteAr = null);

    public const int Weeks = 4;

    private const string FormFirst = "ركّز على الأداء الصحيح قبل زيادة الوزن";
    private const string Steady = "مجهود متوسط تقدر تتكلم فيه";

    private static SampleExercise R(string library, string nameAr, int sets, string reps, int rest, string? note = null) =>
        new(library, nameAr, sets, reps, null, rest, note);

    private static SampleExercise T(string library, string nameAr, int sets, int seconds, int rest, string? note = null) =>
        new(library, nameAr, sets, null, seconds, rest, note);

    public static IReadOnlyList<Sample> All { get; } = new[]
    {
        new Sample("beginner_full_body_3d", "فول بادي للمبتدئين – ٣ أيام", "Beginner full body – 3 days", PlanGoal.GeneralFitness, new[]
        {
            new SampleDay("اليوم أ – جسم كامل", "Day A – Full body", new[]
            {
                R("Goblet Squat", "سكوات بالدمبل (جوبلت)", 3, "10-12", 90, FormFirst),
                R("Machine Chest Press", "ضغط صدر على الجهاز", 3, "10-12", 90),
                R("Lat Pulldown", "سحب عالي (لات)", 3, "10-12", 90),
                R("Dumbbell Shoulder Press", "ضغط كتف بالدمبل", 3, "10", 90),
                T("Plank", "بلانك", 3, 30, 45),
            }),
            new SampleDay("اليوم ب – جسم كامل", "Day B – Full body", new[]
            {
                R("Leg Press", "ليج برس", 3, "12", 90),
                R("Dumbbell Bench Press", "بنش بالدمبل", 3, "10", 90),
                R("Seated Cable Row", "سحب أرضي بالكابل", 3, "12", 90),
                R("Glute Bridge", "رفع الحوض", 3, "12", 60),
                R("Dead Bug", "ديد باج", 3, "10", 45),
            }),
            new SampleDay("اليوم ج – جسم كامل", "Day C – Full body", new[]
            {
                R("Dumbbell Romanian Deadlift", "رومانيان ديدلفت بالدمبل", 3, "10", 90, FormFirst),
                R("Push-Ups", "ضغط (بوش أب)", 3, "8-10", 60),
                R("Dumbbell Row", "سحب دمبل بذراع واحد", 3, "10", 90),
                R("Dumbbell Lunge", "طعن بالدمبل (لانجز)", 3, "10", 90),
                T("Side Plank", "بلانك جانبي", 3, 20, 45),
            }),
        }),
        new Sample("fat_loss_3d", "حرق دهون – ٣ أيام", "Fat loss – 3 days", PlanGoal.FatLoss, new[]
        {
            new SampleDay("اليوم أ – دائري + كارديو", "Day A – Circuit + cardio", new[]
            {
                T("Treadmill Incline Walking", "مشي على المشاية بميل", 1, 600, 60, Steady),
                R("Goblet Squat", "سكوات بالدمبل (جوبلت)", 3, "15", 45),
                R("Push-Ups", "ضغط (بوش أب)", 3, "10", 45),
                R("Seated Cable Row", "سحب أرضي بالكابل", 3, "15", 45),
                T("Mountain Climbers", "متسلق الجبل", 3, 30, 30),
                T("Stationary Bike", "عجلة ثابتة", 1, 600, 0, Steady),
            }),
            new SampleDay("اليوم ب – دائري + كارديو", "Day B – Circuit + cardio", new[]
            {
                T("Rowing Machine", "جهاز التجديف", 1, 300, 60),
                R("Dumbbell Walking Lunge", "لانجز مشي بالدمبل", 3, "12", 45),
                R("Dumbbell Shoulder Press", "ضغط كتف بالدمبل", 3, "12", 45),
                R("Lat Pulldown", "سحب عالي (لات)", 3, "15", 45),
                R("Burpees", "بيربي", 3, "10", 45),
                T("Elliptical Trainer", "الأوربتراك", 1, 900, 0, Steady),
            }),
            new SampleDay("اليوم ج – دائري + كارديو", "Day C – Circuit + cardio", new[]
            {
                R("Kettlebell Swing", "سوينج بالكيتل بل", 3, "15", 45),
                R("Leg Press", "ليج برس", 3, "15", 45),
                R("Machine Chest Press", "ضغط صدر على الجهاز", 3, "12", 45),
                T("Jump Rope", "نط الحبل", 3, 60, 30),
                R("Russian Twist", "روشن تويست", 3, "20", 30),
                T("Treadmill Incline Walking", "مشي على المشاية بميل", 1, 900, 0, Steady),
            }),
        }),
        new Sample("upper_lower_4d", "أبر / لوور – ٤ أيام", "Upper / lower – 4 days", PlanGoal.MuscleGain, new[]
        {
            new SampleDay("اليوم ١ – الجزء العلوي", "Day 1 – Upper body", new[]
            {
                R("Barbell Bench Press", "بنش بار", 4, "6-8", 120, FormFirst),
                R("Bent-Over Barbell Row", "سحب بار منحني", 4, "8", 120),
                R("Dumbbell Shoulder Press", "ضغط كتف بالدمبل", 3, "10", 90),
                R("Lat Pulldown", "سحب عالي (لات)", 3, "10-12", 90),
                R("Barbell Curl", "باي بار", 3, "10-12", 60),
                R("Tricep Rope Pushdown", "تراي حبل", 3, "12", 60),
            }),
            new SampleDay("اليوم ٢ – الجزء السفلي", "Day 2 – Lower body", new[]
            {
                R("Barbell Back Squat", "سكوات بار", 4, "6-8", 150, FormFirst),
                R("Romanian Deadlift", "رومانيان ديدلفت", 3, "8-10", 120),
                R("Leg Press", "ليج برس", 3, "10-12", 90),
                R("Lying Leg Curl Machine", "رجل خلفي نايم", 3, "12", 60),
                R("Standing Calf Raise (Machine)", "سمانة واقف", 4, "12-15", 60),
                T("Plank", "بلانك", 3, 45, 45),
            }),
            new SampleDay("اليوم ٣ – الجزء العلوي", "Day 3 – Upper body", new[]
            {
                R("Incline Dumbbell Bench Press", "بنش عالي بالدمبل", 4, "8-10", 90),
                R("Seated Cable Row", "سحب أرضي بالكابل", 4, "10", 90),
                R("Dumbbell Lateral Raise", "رفرفة جانبي", 3, "12-15", 60),
                R("Pull-Ups", "عقلة", 3, "6-8", 120),
                R("Hammer Curl", "باي هامر", 3, "10-12", 60),
                R("Overhead Cable Tricep Extension", "تراي خلف الرأس بالكابل", 3, "12", 60),
            }),
            new SampleDay("اليوم ٤ – الجزء السفلي", "Day 4 – Lower body", new[]
            {
                R("Conventional Deadlift", "ديدلفت", 3, "5", 180, FormFirst),
                R("Dumbbell Bulgarian Split Squat", "بلغاري سكوات", 3, "10", 90),
                R("Leg Extension Machine", "رجل أمامي", 3, "12-15", 60),
                R("Barbell Hip Thrust", "هيب ثرست", 3, "10", 90),
                R("Seated Calf Raise (Machine)", "سمانة جالس", 4, "15", 60),
                R("Hanging Knee Raises", "رفع الركبة معلّق", 3, "12", 60),
            }),
        }),
    };

    /// <summary>Builds the plan content, linking each exercise to the library when the name is there.</summary>
    public static PlanContent ToContent(Sample sample, IReadOnlyDictionary<string, (Guid Id, string? NameAr)> library)
    {
        var week = new PlanWeek
        {
            Week = 1,
            Days = sample.Days.Select(d => new PlanDay
            {
                Title = d.TitleEn,
                TitleAr = d.TitleAr,
                Exercises = d.Exercises.Select(e => ToExercise(e, library)).ToList()
            }).ToList()
        };
        var document = PlanDocument.Of(new[] { week }).CopyFirstWeekToAll(Weeks);
        return PlanContent.Create(new PlanContent { Name = sample.NameEn, NameAr = sample.NameAr, Goal = sample.Goal, Document = document }).Value;
    }

    private static PlanExercise ToExercise(SampleExercise e, IReadOnlyDictionary<string, (Guid Id, string? NameAr)> library)
    {
        var linked = library.TryGetValue(e.Library, out var match);
        return new PlanExercise
        {
            ExerciseId = linked ? match.Id : null,
            Name = e.Library,
            NameAr = linked && match.NameAr is not null ? match.NameAr : e.NameAr,
            Sets = e.Sets,
            Reps = e.Reps,
            Seconds = e.Seconds,
            RestSeconds = e.Rest,
            Notes = e.NoteAr
        };
    }
}
