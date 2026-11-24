using GymApp.Modules.Training.Application.TrainingPlans.Templates;
using GymApp.Modules.Training.Domain.Enums;
using static GymApp.Modules.Training.Application.TrainingPlans.Programs.ProgramParts;
using static GymApp.Modules.Training.Application.TrainingPlans.Templates.SamplePlanTemplates;

namespace GymApp.Modules.Training.Application.TrainingPlans.Programs;

/// <summary>
/// Programs that fit a season or a life: Ramadan after iftar, training at home or on the coast
/// with nothing but body weight, and an easy-on-the-joints start for 40+.
/// </summary>
internal static class LifestylePrograms
{
    private const string AfterIftar = "اتمرّن بعد الفطار بساعتين تقريباً، واشرب مية بين المجموعات";
    private const string Easy = "مجهود متوسط – مش لحد التعب";
    private const string StopIfPain = "لو حسّيت بألم في المفصل وقّف وكلّم مدربك";
    private const string OnAChair = "استخدم كرسي ثابت";

    private static readonly SampleDay RamadanA = Day("رمضان – اليوم أ", "Ramadan – Day A",
        R("Leg Press", "ليج برس", 3, "12", 90, AfterIftar),
        R("Machine Chest Press", "ضغط صدر على الجهاز", 3, "12", 90),
        R("Lat Pulldown", "سحب عالي (لات)", 3, "12", 90),
        R("Dumbbell Lateral Raise", "رفرفة جانبي", 2, "15", 60),
        T("Plank", "بلانك", 2, 30, 45),
        T("Treadmill Walking", "مشي على المشاية", 1, 600, 0, Easy));

    private static readonly SampleDay RamadanB = Day("رمضان – اليوم ب", "Ramadan – Day B",
        R("Goblet Squat", "سكوات بالدمبل (جوبلت)", 3, "12", 90, AfterIftar),
        R("Dumbbell Bench Press", "بنش بالدمبل", 3, "10", 90),
        R("Seated Cable Row", "سحب أرضي بالكابل", 3, "12", 90),
        R("Dumbbell Curl", "باي دمبل", 2, "12", 60),
        R("Tricep Rope Pushdown", "تراي حبل", 2, "12", 60),
        T("Stationary Bike", "عجلة ثابتة", 1, 600, 0, Easy));

    private static readonly SampleDay RamadanC = Day("رمضان – اليوم ج", "Ramadan – Day C",
        R("Dumbbell Romanian Deadlift", "رومانيان ديدلفت بالدمبل", 3, "10", 90, AfterIftar),
        R("Incline Dumbbell Bench Press", "بنش عالي بالدمبل", 3, "10", 90),
        R("Machine Row", "سحب على الجهاز", 3, "12", 90),
        R("Machine Shoulder Press", "ضغط كتف على الجهاز", 2, "12", 60),
        R("Dead Bug", "ديد باج", 2, "10", 45),
        T("Elliptical Trainer", "الأوربتراك", 1, 600, 0, Easy));

    private static readonly SampleDay HomeA = Day("البيت – اليوم أ", "Home – Day A",
        T("Jumping Jacks", "جامبينج جاكس", 3, 45, 30),
        R("Push-Ups", "ضغط (بوش أب)", 3, "8-12", 60, "على الركبة لو صعب"),
        R("Jump Squat", "سكوات بنطة", 3, "10", 60),
        R("Pike Push-Ups", "بايك بوش أب (كتف)", 3, "8", 60),
        R("Glute Bridge", "رفع الحوض", 3, "15", 45),
        T("Plank", "بلانك", 3, 30, 30));

    private static readonly SampleDay HomeB = Day("البيت – اليوم ب", "Home – Day B",
        T("High Knees", "رفع الركبة جري في المكان", 3, 30, 30),
        T("Wall Sit", "قعدة الحيطة", 3, 30, 45),
        R("Wide Push-Ups", "ضغط واسع", 3, "10", 60),
        R("Bench Dips", "تراي على الكرسي", 3, "10", 60, OnAChair),
        R("Bodyweight Calf Raise", "سمانة بوزن الجسم", 3, "20", 30),
        R("Bicycle Crunches", "بطن عجلة", 3, "20", 30));

    private static readonly SampleDay HomeC = Day("البيت – اليوم ج", "Home – Day C",
        R("Burpees", "بيربي", 3, "8", 60),
        R("Single-Leg Glute Bridge", "رفع الحوض برجل واحدة", 3, "12", 45, "١٢ لكل رجل"),
        R("Diamond Push-Ups", "ضغط دايموند", 3, "6-8", 60),
        T("Mountain Climbers", "متسلق الجبل", 3, 30, 30),
        R("Bird Dog", "بيرد دوج", 3, "10", 30),
        T("Side Plank", "بلانك جانبي", 3, 20, 30));

    private static readonly SampleDay JointsA = Day("اليوم أ – أجهزة", "Day A – machines",
        T("Recumbent Bike", "عجلة بضهر", 1, 600, 60, "إحماء"),
        R("Leg Press", "ليج برس", 2, "12-15", 90, StopIfPain),
        R("Machine Chest Press", "ضغط صدر على الجهاز", 2, "12", 90),
        R("Chest-Supported Row Machine", "سحب على الجهاز بسند صدر", 2, "12", 90),
        R("Glute Bridge", "رفع الحوض", 2, "12", 60),
        R("Bird Dog", "بيرد دوج", 2, "10", 45),
        T("Cat-Cow Stretch", "إطالة القطة والجمل", 1, 60, 0));

    private static readonly SampleDay JointsB = Day("اليوم ب – توازن وقوة", "Day B – balance & strength",
        T("Elliptical Trainer", "الأوربتراك", 1, 600, 60, "إحماء"),
        R("Goblet Squat", "سكوات بالدمبل (جوبلت)", 2, "10", 90, "انزل لحد الكرسي بس"),
        R("Lat Pulldown", "سحب عالي (لات)", 2, "12", 90),
        R("Machine Shoulder Press", "ضغط كتف على الجهاز", 2, "12", 90, StopIfPain),
        R("Seated Leg Curl Machine", "رجل خلفي جالس", 2, "12", 60),
        R("Dead Bug", "ديد باج", 2, "8", 45),
        T("Hip Flexor Stretch", "إطالة الحوض الأمامية", 2, 30, 15));

    private static readonly SampleDay JointsC = Day("اليوم ج – مشي وتقوية", "Day C – walk & strengthen",
        T("Treadmill Walking", "مشي على المشاية", 1, 900, 60, Easy),
        R("Dumbbell Step-Up", "طلوع على بوكس بالدمبل", 2, "10", 90, "١٠ لكل رجل"),
        R("Cable Face Pull", "فيس بول", 2, "15", 60),
        R("Dumbbell Curl", "باي دمبل", 2, "12", 60),
        R("Tricep Pushdown (Cable)", "تراي كابل", 2, "12", 60),
        T("Side Plank", "بلانك جانبي", 2, 15, 30),
        T("Hamstring Stretch", "إطالة الخلفية", 2, 30, 15));

    public static IReadOnlyList<ReadyProgram> All { get; } = new[]
    {
        new ReadyProgram("ramadan_after_iftar_3d",
            new ProgramText("رمضان بعد الفطار – ٣ أيام", "Ramadan after iftar – 3 days",
                "حوالي ٤٠ دقيقة بمجهود أخف، تحافظ بيه على عضلك ولياقتك طول الشهر.",
                "About 40 minutes at lighter effort to keep muscle and fitness through the month."),
            new ProgramProfile(ProgramCategory.Lifestyle, ProgramLevel.Beginner, PlanGoal.GeneralFitness, 4),
            EveryWeek(RamadanA, RamadanB, RamadanC)),
        new ReadyProgram("home_no_equipment_3d",
            new ProgramText("في البيت من غير أدوات – ٣ أيام", "Home, no equipment – 3 days",
                "بوزن الجسم بس: للسفر والمصيف أو الأيام اللي مش هتنزل فيها الجيم.",
                "Body weight only: for travel, the summer coast, or days away from the gym."),
            new ProgramProfile(ProgramCategory.Lifestyle, ProgramLevel.Beginner, PlanGoal.GeneralFitness, 4),
            EveryWeek(HomeA, HomeB, HomeC)),
        new ReadyProgram("joint_friendly_40plus_3d",
            new ProgramText("بداية مريحة للمفاصل ٤٠+ – ٣ أيام", "Joint-friendly start 40+ – 3 days",
                "أجهزة ومجموعات قليلة وإطالات، لبداية آمنة بعد سن الأربعين.",
                "Machines, few sets and stretching for a safe start after forty."),
            new ProgramProfile(ProgramCategory.Lifestyle, ProgramLevel.Beginner, PlanGoal.GeneralFitness, 8),
            EveryWeek(JointsA, JointsB, JointsC)),
    };
}
