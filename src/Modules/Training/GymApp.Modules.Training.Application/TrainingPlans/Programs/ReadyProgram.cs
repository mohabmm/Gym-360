using GymApp.Modules.Training.Application.TrainingPlans.Templates;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.ValueObjects;
using static GymApp.Modules.Training.Application.TrainingPlans.Templates.SamplePlanTemplates;

namespace GymApp.Modules.Training.Application.TrainingPlans.Programs;

/// <summary>Where a ready program comes from, for the coach's filter chips.</summary>
public enum ProgramCategory
{
    Classic = 0,
    Egyptian = 1,
    Lifestyle = 2,
    Cardio = 3
}

public enum ProgramLevel
{
    Beginner = 0,
    Intermediate = 1
}

/// <summary>Name and one-line description, Arabic first.</summary>
internal sealed record ProgramText(string NameAr, string NameEn, string DescriptionAr, string DescriptionEn);

/// <summary>Who the program is for and how long it runs.</summary>
internal sealed record ProgramProfile(ProgramCategory Category, ProgramLevel Level, PlanGoal Goal, int Weeks);

/// <summary>
/// pt.training_plan: a built-in program every coach can start from (برامج جاهزة). The same for
/// every gym, so it lives in code; using it copies it into a plan or the coach's own template.
/// <see cref="WeekPattern"/> repeats in order: one week means every week is the same; two weeks
/// alternate (A/B/A then B/A/B); a pattern as long as <see cref="ProgramProfile.Weeks"/> progresses
/// week by week.
/// </summary>
internal sealed record ReadyProgram(string Key, ProgramText Text, ProgramProfile Profile, IReadOnlyList<IReadOnlyList<SampleDay>> WeekPattern)
{
    public int DaysPerWeek => WeekPattern.Max(w => w.Count);

    /// <summary>The program as plan content, its exercises linked to the gym's library by name.</summary>
    public PlanContent ToContent(IReadOnlyDictionary<string, (Guid Id, string? NameAr)> library)
    {
        var weeks = Enumerable.Range(0, Profile.Weeks).Select(i => new PlanWeek
        {
            Week = i + 1,
            Days = WeekPattern[i % WeekPattern.Count].Select(d => ToDay(d, library)).ToList()
        });
        var draft = new PlanContent { Name = Text.NameEn, NameAr = Text.NameAr, Goal = Profile.Goal, Document = PlanDocument.Of(weeks) };
        return PlanContent.Create(draft).Value;
    }
}

/// <summary>Short builders for the catalog files (exercises: <c>SamplePlanTemplates.R</c> / <c>T</c>).</summary>
internal static class ProgramParts
{
    public static SampleDay Day(string titleAr, string titleEn, params SampleExercise[] exercises) => new(titleAr, titleEn, exercises);

    public static IReadOnlyList<IReadOnlyList<SampleDay>> EveryWeek(params SampleDay[] days) => new[] { days };
}
