using GymApp.Modules.Training.Application.TrainingPlans.Templates;
using GymApp.Modules.Training.Domain.Enums;
using static GymApp.Modules.Training.Application.TrainingPlans.Programs.ProgramParts;
using static GymApp.Modules.Training.Application.TrainingPlans.Templates.SamplePlanTemplates;

namespace GymApp.Modules.Training.Application.TrainingPlans.Programs;

/// <summary>
/// Programs known in gyms worldwide, under plain names: the three gym samples, a 5×5 beginner
/// strength program, push/pull/legs (3 and 6 days), the Arnold split and a power + hypertrophy
/// upper/lower split.
/// </summary>
internal static class ClassicPrograms
{
    private const string AddWeight = "لو كمّلت كل المجموعات، زوّد ٢٫٥ كجم المرة الجاية";
    private const string AddWeightDeadlift = "لو كمّلت، زوّد ٥ كجم المرة الجاية";
    private const string Heavy = "تقيل مع أداء نضيف – ارتاح كويس بين المجموعات";

    private static readonly IReadOnlyDictionary<string, ProgramText> SampleTexts = new Dictionary<string, ProgramText>
    {
        ["beginner_full_body_3d"] = new("فول بادي للمبتدئين – ٣ أيام", "Beginner full body – 3 days",
            "أول شهر في الجيم: الجسم كله ٣ مرات في الأسبوع بأجهزة ودمبل.", "First month in the gym: the whole body three times a week, machines and dumbbells."),
        ["fat_loss_3d"] = new("حرق دهون – ٣ أيام", "Fat loss – 3 days",
            "تمارين دائرية براحة قصيرة وكارديو في الآخر.", "Circuits with short rests, finished with cardio."),
        ["upper_lower_4d"] = new("أبر / لوور – ٤ أيام", "Upper / lower – 4 days",
            "جزء علوي وسفلي مرتين في الأسبوع لبناء العضل.", "Upper and lower body twice a week to build muscle."),
    };

    private static ReadyProgram FromSample(SamplePlanTemplates.Sample s) => new(
        s.Key,
        SampleTexts[s.Key],
        new ProgramProfile(ProgramCategory.Classic, s.Key == "upper_lower_4d" ? ProgramLevel.Intermediate : ProgramLevel.Beginner, s.Goal, SamplePlanTemplates.Weeks),
        EveryWeek(s.Days.ToArray()));

    private static readonly SampleDay FiveA = Day("التمرين أ", "Workout A",
        R("Barbell Back Squat", "سكوات بار", 5, "5", 180, AddWeight),
        R("Barbell Bench Press", "بنش بار", 5, "5", 180, AddWeight),
        R("Bent-Over Barbell Row", "سحب بار منحني", 5, "5", 180, AddWeight));

    private static readonly SampleDay FiveB = Day("التمرين ب", "Workout B",
        R("Barbell Back Squat", "سكوات بار", 5, "5", 180, AddWeight),
        R("Overhead Press (Military Press)", "ضغط كتف بالبار واقف", 5, "5", 180, AddWeight),
        R("Conventional Deadlift", "ديدلفت", 1, "5", 180, AddWeightDeadlift));

    private static readonly SampleDay PushA = Day("دفع – صدر وكتف وتراي", "Push – chest, shoulders, triceps",
        R("Barbell Bench Press", "بنش بار", 4, "6-8", 150, Heavy),
        R("Incline Dumbbell Bench Press", "بنش عالي بالدمبل", 3, "8-10", 90),
        R("Dumbbell Shoulder Press", "ضغط كتف بالدمبل", 3, "10", 90),
        R("Dumbbell Lateral Raise", "رفرفة جانبي", 3, "12-15", 60),
        R("Tricep Rope Pushdown", "تراي حبل", 3, "12", 60),
        R("Overhead Cable Tricep Extension", "تراي خلف الرأس بالكابل", 3, "12", 60));

    private static readonly SampleDay PullA = Day("سحب – ضهر وباي", "Pull – back, biceps",
        R("Conventional Deadlift", "ديدلفت", 3, "5", 180, Heavy),
        R("Lat Pulldown", "سحب عالي (لات)", 3, "8-10", 90),
        R("Seated Cable Row", "سحب أرضي بالكابل", 3, "10", 90),
        R("Cable Face Pull", "فيس بول", 3, "15", 60),
        R("Barbell Curl", "باي بار", 3, "10", 60),
        R("Hammer Curl", "باي هامر", 3, "12", 60));

    private static readonly SampleDay LegsA = Day("رجل", "Legs",
        R("Barbell Back Squat", "سكوات بار", 4, "6-8", 150, Heavy),
        R("Romanian Deadlift", "رومانيان ديدلفت", 3, "8-10", 120),
        R("Leg Press", "ليج برس", 3, "12", 90),
        R("Lying Leg Curl Machine", "رجل خلفي نايم", 3, "12", 60),
        R("Standing Calf Raise (Machine)", "سمانة واقف", 4, "12-15", 60),
        R("Hanging Knee Raises", "رفع الركبة معلّق", 3, "12", 60));

    private static readonly SampleDay PushB = Day("دفع ٢ – كتف وصدر عالي", "Push 2 – shoulders, upper chest",
        R("Overhead Press (Military Press)", "ضغط كتف بالبار واقف", 4, "6-8", 150, Heavy),
        R("Incline Barbell Bench Press", "بنش عالي بالبار", 3, "8-10", 120),
        R("Cable Fly", "فلاي كابل", 3, "12-15", 60),
        R("Cable Lateral Raise", "رفرفة جانبي كابل", 3, "15", 60),
        R("Skull Crusher (EZ-Bar)", "تراي جبهة بالبار", 3, "10", 60),
        R("Tricep Dips", "متوازي تراي", 3, "8-12", 90));

    private static readonly SampleDay PullB = Day("سحب ٢ – عرض الضهر", "Pull 2 – back width",
        R("Pull-Ups", "عقلة", 4, "6-10", 120),
        R("Bent-Over Barbell Row", "سحب بار منحني", 4, "8", 120),
        R("Single-Arm Cable Row", "سحب كابل بذراع واحد", 3, "12", 60),
        R("Dumbbell Rear Delt Fly", "كتف خلفي بالدمبل", 3, "15", 60),
        R("Incline Dumbbell Curl", "باي دمبل على بنش عالي", 3, "10", 60),
        R("Cable Curl", "باي كابل", 3, "12", 60));

    private static readonly SampleDay LegsB = Day("رجل ٢ – أمامي وجلوتس", "Legs 2 – quads, glutes",
        R("Barbell Front Squat", "سكوات أمامي بالبار", 4, "6-8", 150, Heavy),
        R("Barbell Hip Thrust", "هيب ثرست", 3, "10", 90),
        R("Dumbbell Walking Lunge", "لانجز مشي بالدمبل", 3, "12", 90),
        R("Seated Leg Curl Machine", "رجل خلفي جالس", 3, "12", 60),
        R("Leg Extension Machine", "رجل أمامي", 3, "15", 60),
        R("Seated Calf Raise (Machine)", "سمانة جالس", 4, "15", 60));

    private static readonly SampleDay UpperPower = Day("علوي – قوة", "Upper – power",
        R("Barbell Bench Press", "بنش بار", 4, "3-5", 180, Heavy),
        R("Bent-Over Barbell Row", "سحب بار منحني", 4, "3-5", 180, Heavy),
        R("Overhead Press (Military Press)", "ضغط كتف بالبار واقف", 3, "5-8", 120),
        R("Pull-Ups", "عقلة", 3, "6-8", 120),
        R("Barbell Curl", "باي بار", 2, "6-10", 90),
        R("Skull Crusher (EZ-Bar)", "تراي جبهة بالبار", 2, "6-10", 90));

    private static readonly SampleDay LowerPower = Day("سفلي – قوة", "Lower – power",
        R("Barbell Back Squat", "سكوات بار", 4, "3-5", 180, Heavy),
        R("Conventional Deadlift", "ديدلفت", 3, "3-5", 180, Heavy),
        R("Leg Press", "ليج برس", 3, "10-15", 90),
        R("Lying Leg Curl Machine", "رجل خلفي نايم", 3, "6-10", 90),
        R("Standing Calf Raise (Machine)", "سمانة واقف", 4, "6-10", 60));

    private static readonly SampleDay UpperVolume = Day("علوي – تضخيم", "Upper – hypertrophy",
        R("Incline Dumbbell Bench Press", "بنش عالي بالدمبل", 4, "8-12", 90),
        R("Cable Fly", "فلاي كابل", 3, "12-15", 60),
        R("Seated Cable Row", "سحب أرضي بالكابل", 4, "8-12", 90),
        R("Dumbbell Row", "سحب دمبل بذراع واحد", 3, "10-12", 60),
        R("Dumbbell Lateral Raise", "رفرفة جانبي", 3, "12-15", 60),
        R("Incline Dumbbell Curl", "باي دمبل على بنش عالي", 3, "10-12", 60),
        R("Tricep Rope Pushdown", "تراي حبل", 3, "10-12", 60));

    private static readonly SampleDay LowerVolume = Day("سفلي – تضخيم", "Lower – hypertrophy",
        R("Barbell Front Squat", "سكوات أمامي بالبار", 4, "8-12", 120),
        R("Dumbbell Bulgarian Split Squat", "بلغاري سكوات", 3, "10", 90),
        R("Leg Extension Machine", "رجل أمامي", 3, "12-15", 60),
        R("Seated Leg Curl Machine", "رجل خلفي جالس", 3, "12-15", 60),
        R("Seated Calf Raise (Machine)", "سمانة جالس", 4, "12-15", 60),
        R("Cable Crunch", "بطن كابل", 3, "15", 60));

    private static readonly SampleDay ArnoldChestBack = Day("صدر وضهر", "Chest & back",
        R("Barbell Bench Press", "بنش بار", 4, "8-10", 90),
        R("Pull-Ups", "عقلة", 4, "8-10", 90),
        R("Incline Dumbbell Bench Press", "بنش عالي بالدمبل", 3, "10", 90),
        R("Bent-Over Barbell Row", "سحب بار منحني", 3, "10", 90),
        R("Dumbbell Fly", "فلاي بالدمبل", 3, "12", 60),
        R("Dumbbell Pullover", "بول أوفر بالدمبل", 3, "12", 60));

    private static readonly SampleDay ArnoldShouldersArms = Day("كتف ودراع", "Shoulders & arms",
        R("Seated Barbell Press", "ضغط كتف بالبار جالس", 4, "8-10", 90),
        R("Arnold Press", "أرنولد برس", 3, "10", 90),
        R("Dumbbell Lateral Raise", "رفرفة جانبي", 3, "12-15", 60),
        R("Barbell Curl", "باي بار", 3, "10", 60),
        R("Close-Grip Bench Press", "بنش ضيق (تراي)", 3, "10", 90),
        R("Incline Dumbbell Curl", "باي دمبل على بنش عالي", 3, "10", 60),
        R("Tricep Rope Pushdown", "تراي حبل", 3, "12", 60));

    private static readonly SampleDay ArnoldLegs = Day("رجل وبطن", "Legs & abs",
        R("Barbell Back Squat", "سكوات بار", 4, "8-10", 150),
        R("Romanian Deadlift", "رومانيان ديدلفت", 3, "10", 120),
        R("Leg Press", "ليج برس", 3, "12", 90),
        R("Lying Leg Curl Machine", "رجل خلفي نايم", 3, "12", 60),
        R("Standing Calf Raise (Machine)", "سمانة واقف", 4, "15", 60),
        R("Hanging Leg Raises", "رفع الرجل معلّق", 3, "12", 60));

    public static IReadOnlyList<ReadyProgram> All { get; } = SamplePlanTemplates.All.Select(FromSample).Concat(new[]
    {
        new ReadyProgram("strength_5x5_3d",
            new ProgramText("قوة ٥×٥ للمبتدئين – ٣ أيام", "5×5 beginner strength – 3 days",
                "تمرينين أ و ب بالتبادل، ٣ تمارين أساسية بالبار، والوزن يزيد كل حصة.",
                "Two workouts, A and B, alternated: three barbell basics, adding weight every session."),
            new ProgramProfile(ProgramCategory.Classic, ProgramLevel.Beginner, PlanGoal.MuscleGain, 12),
            new IReadOnlyList<SampleDay>[] { new[] { FiveA, FiveB, FiveA }, new[] { FiveB, FiveA, FiveB } }),
        new ReadyProgram("ppl_3d",
            new ProgramText("دفع / سحب / رجل – ٣ أيام", "Push / pull / legs – 3 days",
                "كل عضلة مرة في الأسبوع بتقسيم دفع وسحب ورجل.", "Each muscle once a week, split into push, pull and legs."),
            new ProgramProfile(ProgramCategory.Classic, ProgramLevel.Beginner, PlanGoal.MuscleGain, 8),
            EveryWeek(PushA, PullA, LegsA)),
        new ReadyProgram("ppl_6d",
            new ProgramText("دفع / سحب / رجل – ٦ أيام", "Push / pull / legs – 6 days",
                "كل عضلة مرتين في الأسبوع؛ للي بيتمرن بانتظام من فترة.", "Each muscle twice a week, for people already training regularly."),
            new ProgramProfile(ProgramCategory.Classic, ProgramLevel.Intermediate, PlanGoal.MuscleGain, 8),
            EveryWeek(PushA, PullA, LegsA, PushB, PullB, LegsB)),
        new ReadyProgram("arnold_split_6d",
            new ProgramText("تقسيمة أرنولد – ٦ أيام", "Arnold split – 6 days",
                "صدر وضهر، كتف ودراع، رجل – مرتين في الأسبوع بأسلوب الكمال الأجسام الكلاسيكي.",
                "Chest & back, shoulders & arms, legs – twice a week, classic bodybuilding style."),
            new ProgramProfile(ProgramCategory.Classic, ProgramLevel.Intermediate, PlanGoal.MuscleGain, 8),
            EveryWeek(ArnoldChestBack, ArnoldShouldersArms, ArnoldLegs, ArnoldChestBack, ArnoldShouldersArms, ArnoldLegs)),
        new ReadyProgram("power_hypertrophy_4d",
            new ProgramText("قوة + تضخيم – ٤ أيام", "Power + hypertrophy – 4 days",
                "يومين تقال للقوة ويومين عدّات أعلى للحجم، علوي وسفلي.", "Two heavy days for strength and two higher-rep days for size, upper and lower."),
            new ProgramProfile(ProgramCategory.Classic, ProgramLevel.Intermediate, PlanGoal.MuscleGain, 8),
            EveryWeek(UpperPower, LowerPower, UpperVolume, LowerVolume)),
    }).ToList();
}
