using GymApp.Modules.Training.Application.TrainingPlans.Templates;
using GymApp.Modules.Training.Domain.Enums;
using static GymApp.Modules.Training.Application.TrainingPlans.Programs.ProgramParts;
using static GymApp.Modules.Training.Application.TrainingPlans.Templates.SamplePlanTemplates;

namespace GymApp.Modules.Training.Application.TrainingPlans.Programs;

/// <summary>
/// What Egyptian gyms run every day: «التقسيمة» (one muscle group a day), the pre-summer
/// «تنشيف», and glutes and legs for the ladies' hours. Arabic as coaches say it on the floor.
/// </summary>
internal static class EgyptianPrograms
{
    private const string WarmUp = "ابدأ بإحماء خفيف ٥–١٠ دقايق";
    private const string Squeeze = "اعصر العضلة فوق ثانية";
    private const string LessRest = "قلّل الراحة شوية كل أسبوع";
    private const string GluteSqueeze = "ركّزي على عصر الجلوتس فوق";

    private static readonly SampleDay Chest = Day("صدر", "Chest",
        R("Barbell Bench Press", "بنش مستوي بالبار", 4, "8-10", 120, WarmUp),
        R("Incline Dumbbell Bench Press", "بنش عالي دمبل", 4, "10", 90),
        R("Decline Barbell Bench Press", "بنش واطي بالبار", 3, "10", 90),
        R("Pec Deck Machine", "بيك دك (فراشة)", 3, "12", 60, Squeeze),
        R("Cable Crossover", "كروس أوفر", 3, "12-15", 60),
        R("Chest Dips", "متوازي صدر", 3, "10", 90));

    private static readonly SampleDay Back = Day("ضهر", "Back",
        R("Wide-Grip Lat Pulldown", "سحب أمامي واسع", 4, "10", 90, WarmUp),
        R("Bent-Over Barbell Row", "سحب بار منحني", 4, "8-10", 90),
        R("T-Bar Row", "تي بار", 3, "10", 90),
        R("Seated Cable Row", "سحب أرضي", 3, "12", 60),
        R("Straight-Arm Pulldown", "بول أوفر كابل", 3, "12-15", 60),
        R("Back Extension", "ضهر سفلي", 3, "15", 60));

    private static readonly SampleDay Shoulders = Day("كتف", "Shoulders",
        R("Seated Dumbbell Press", "ضغط كتف دمبل جالس", 4, "10", 90, WarmUp),
        R("Dumbbell Lateral Raise", "رفرفة جانبي", 4, "12-15", 60),
        R("Dumbbell Front Raise", "رفرفة أمامي", 3, "12", 60),
        R("Machine Rear Delt Fly", "كتف خلفي جهاز", 3, "15", 60),
        R("Barbell Shrug", "ترابيس بار", 4, "12", 60));

    private static readonly SampleDay Legs = Day("رجل", "Legs",
        R("Barbell Back Squat", "سكوات بار", 4, "8-10", 150, WarmUp),
        R("Leg Press", "ليج برس", 4, "12", 90),
        R("Leg Extension Machine", "رجل أمامي", 3, "15", 60),
        R("Lying Leg Curl Machine", "رجل خلفي", 3, "12", 60),
        R("Dumbbell Walking Lunge", "لانجز مشي بالدمبل", 3, "12", 90),
        R("Standing Calf Raise (Machine)", "سمانة واقف", 4, "15", 60));

    private static readonly SampleDay Arms = Day("دراع – باي وتراي", "Arms – biceps, triceps",
        R("Barbell Curl", "باي بار", 4, "10", 60),
        R("Tricep Rope Pushdown", "تراي حبل", 4, "12", 60),
        R("Hammer Curl", "باي هامر", 3, "12", 60),
        R("Skull Crusher (EZ-Bar)", "تراي بار جبهة", 3, "10", 60),
        R("Concentration Curl", "باي تركيز", 3, "12", 60, Squeeze),
        R("Overhead Cable Tricep Extension", "تراي خلف الرأس بالكابل", 3, "12", 60));

    private static readonly SampleDay ShredChest = Day("صدر وتراي + كارديو", "Chest & triceps + cardio",
        R("Incline Dumbbell Bench Press", "بنش عالي دمبل", 4, "12", 60, LessRest),
        R("Machine Chest Press", "ضغط صدر على الجهاز", 3, "15", 45),
        R("Cable Crossover", "كروس أوفر", 3, "15", 45),
        R("Tricep Rope Pushdown", "تراي حبل", 3, "15", 45),
        T("Treadmill Incline Walking", "مشي على المشاية بميل", 1, 1200, 0));

    private static readonly SampleDay ShredBack = Day("ضهر وباي + كارديو", "Back & biceps + cardio",
        R("Lat Pulldown", "سحب عالي (لات)", 4, "12", 60, LessRest),
        R("Seated Cable Row", "سحب أرضي", 3, "15", 45),
        R("Dumbbell Row", "سحب دمبل بذراع واحد", 3, "12", 45),
        R("Cable Curl", "باي كابل", 3, "15", 45),
        T("Stair Climber", "جهاز السلالم", 1, 900, 0));

    private static readonly SampleDay ShredLegs = Day("رجل + حبل", "Legs + jump rope",
        R("Goblet Squat", "سكوات بالدمبل (جوبلت)", 4, "15", 60),
        R("Leg Press", "ليج برس", 3, "15", 60),
        R("Dumbbell Walking Lunge", "لانجز مشي بالدمبل", 3, "12", 60),
        R("Lying Leg Curl Machine", "رجل خلفي", 3, "15", 45),
        R("Bodyweight Calf Raise", "سمانة بوزن الجسم", 3, "20", 30),
        T("Jump Rope", "نط الحبل", 5, 60, 30));

    private static readonly SampleDay ShredShoulders = Day("كتف وبطن", "Shoulders & abs",
        R("Dumbbell Shoulder Press", "ضغط كتف بالدمبل", 4, "12", 60),
        R("Dumbbell Lateral Raise", "رفرفة جانبي", 4, "15", 45),
        R("Cable Face Pull", "فيس بول", 3, "15", 45),
        R("Hanging Knee Raises", "رفع الركبة معلّق", 3, "15", 45),
        R("Bicycle Crunches", "بطن عجلة", 3, "20", 30),
        T("Plank", "بلانك", 3, 45, 30));

    private static readonly SampleDay ShredCircuit = Day("دائري للجسم كله", "Full-body circuit",
        R("Kettlebell Swing", "سوينج بالكيتل بل", 4, "15", 30),
        R("Burpees", "بيربي", 4, "10", 30),
        R("Push-Ups", "ضغط (بوش أب)", 4, "12", 30),
        T("Mountain Climbers", "متسلق الجبل", 4, 30, 30),
        T("Battle Ropes", "حبال المعركة", 4, 30, 30),
        T("Rowing Machine", "جهاز التجديف", 1, 600, 0));

    private static readonly SampleDay LadiesGlutes = Day("جلوتس", "Glutes",
        R("Barbell Hip Thrust", "هيب ثرست", 4, "10-12", 90, GluteSqueeze),
        R("Sumo Squat", "سومو سكوات", 3, "12", 60),
        R("Cable Kickback", "كيك باك كابل", 3, "15", 45, "١٥ لكل رجل"),
        R("Machine Hip Abduction", "جهاز الأبدكتور (خارجي)", 3, "15-20", 45),
        R("Dumbbell Romanian Deadlift", "رومانيان ديدلفت بالدمبل", 3, "12", 60),
        T("Plank", "بلانك", 3, 30, 30));

    private static readonly SampleDay LadiesUpper = Day("جزء علوي وبطن", "Upper body & core",
        R("Lat Pulldown", "سحب عالي (لات)", 3, "12", 60),
        R("Machine Chest Press", "ضغط صدر على الجهاز", 3, "12", 60),
        R("Seated Cable Row", "سحب أرضي", 3, "12", 60),
        R("Dumbbell Shoulder Press", "ضغط كتف بالدمبل", 3, "12", 60),
        R("Dead Bug", "ديد باج", 3, "10", 45),
        T("Treadmill Incline Walking", "مشي على المشاية بميل", 1, 900, 0));

    private static readonly SampleDay LadiesLegs = Day("رجل", "Legs",
        R("Leg Press", "ليج برس", 4, "12", 90),
        R("Dumbbell Bulgarian Split Squat", "بلغاري سكوات", 3, "10", 60, "١٠ لكل رجل"),
        R("Glute Bridge", "رفع الحوض", 3, "15", 45, GluteSqueeze),
        R("Lying Leg Curl Machine", "رجل خلفي", 3, "12", 60),
        R("Banded Lateral Walk", "مشي جانبي بالأستك", 3, "12", 45, "١٢ خطوة لكل ناحية"),
        T("Stair Climber", "جهاز السلالم", 1, 600, 0));

    public static IReadOnlyList<ReadyProgram> All { get; } = new[]
    {
        new ReadyProgram("classic_split_5d",
            new ProgramText("التقسيمة – ٥ أيام (صدر، ضهر، كتف، رجل، دراع)", "Classic split – 5 days (chest, back, shoulders, legs, arms)",
                "التقسيمة المشهورة في الجيمات: عضلة كل يوم بتمارين أجهزة وأوزان حرة.",
                "The split every gym knows: one muscle group a day, machines and free weights."),
            new ProgramProfile(ProgramCategory.Egyptian, ProgramLevel.Intermediate, PlanGoal.MuscleGain, 8),
            EveryWeek(Chest, Back, Shoulders, Legs, Arms)),
        new ReadyProgram("summer_shred_5d",
            new ProgramText("تنشيف الصيف – ٥ أيام", "Summer shred – 5 days",
                "عدّات عالية وراحة قصيرة وكارديو كل يوم قبل الصيف؛ مع نظام أكل مناسب.",
                "High reps, short rests and cardio every day before summer, with the right diet."),
            new ProgramProfile(ProgramCategory.Egyptian, ProgramLevel.Intermediate, PlanGoal.FatLoss, 6),
            EveryWeek(ShredChest, ShredBack, ShredLegs, ShredShoulders, ShredCircuit)),
        new ReadyProgram("ladies_glutes_legs_3d",
            new ProgramText("جلوتس ورجل للسيدات – ٣ أيام", "Glutes & legs for ladies – 3 days",
                "شد الجسم وتقوية الجلوتس والرجل، مع يوم للجزء العلوي.",
                "Tone up and build glutes and legs, with one upper-body day."),
            new ProgramProfile(ProgramCategory.Egyptian, ProgramLevel.Beginner, PlanGoal.GeneralFitness, 8),
            EveryWeek(LadiesGlutes, LadiesUpper, LadiesLegs)),
    };
}
