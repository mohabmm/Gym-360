using GymApp.Modules.Training.Application.TrainingPlans.Templates;
using GymApp.Modules.Training.Domain.Enums;
using static GymApp.Modules.Training.Application.TrainingPlans.Programs.ProgramParts;
using static GymApp.Modules.Training.Application.TrainingPlans.Templates.SamplePlanTemplates;

namespace GymApp.Modules.Training.Application.TrainingPlans.Programs;

/// <summary>
/// Walk-to-run on the treadmill: 8 weeks, 3 days a week. Each week runs longer between brisk
/// walks (the rest of an interval is the walk), ending with 20 minutes of easy running.
/// </summary>
internal static class CardioPrograms
{
    private const string Easy = "جري خفيف تقدر تتكلم فيه؛ الراحة = مشي سريع";

    /// <summary>(run seconds, times, walk seconds) for weeks 1..8.</summary>
    private static readonly (int Run, int Times, int Walk)[] Weeks =
    {
        (60, 8, 90), (90, 6, 120), (120, 5, 90), (180, 4, 90),
        (300, 3, 180), (480, 2, 180), (600, 2, 120), (1200, 1, 0),
    };

    private static SampleDay RunDay(int day, (int Run, int Times, int Walk) week) => Day(
        $"جري – اليوم {"١٢٣"[day - 1]}", $"Run – Day {day}",
        T("Treadmill Walking", "مشي على المشاية", 1, 300, 60, "إحماء مشي سريع"),
        T("Treadmill Running", "جري على المشاية", week.Times, week.Run, week.Walk, Easy),
        T("Treadmill Walking", "مشي على المشاية", 1, 300, 0, "تهدئة"),
        T("Standing Calf Stretch", "إطالة السمانة", 2, 30, 15),
        T("Hamstring Stretch", "إطالة الخلفية", 2, 30, 15));

    public static IReadOnlyList<ReadyProgram> All { get; } = new[]
    {
        new ReadyProgram("walk_to_run_8w",
            new ProgramText("من المشي للجري – ٨ أسابيع", "Walk to run – 8 weeks",
                "من دقيقة جري لحد ٢٠ دقيقة متواصلة على المشاية، ٣ مرات في الأسبوع.",
                "From one minute of running to 20 minutes non-stop on the treadmill, three times a week."),
            new ProgramProfile(ProgramCategory.Cardio, ProgramLevel.Beginner, PlanGoal.FatLoss, Weeks.Length),
            Weeks.Select(w => (IReadOnlyList<SampleDay>)new[] { RunDay(1, w), RunDay(2, w), RunDay(3, w) }).ToList()),
    };
}
