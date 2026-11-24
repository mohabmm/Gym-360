using GymApp.Modules.Training.Application.TrainingPlans.Programs;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Tests.Application.Training;

/// <summary>
/// The ready programs are content: every one must build into a valid plan, run its stated weeks
/// and days, and keep Arabic text for an Arabic-first coach.
/// </summary>
public class ProgramCatalogTests
{
    private static readonly IReadOnlyDictionary<string, (Guid Id, string? NameAr)> NoLibrary =
        new Dictionary<string, (Guid, string?)>();

    public static TheoryData<string> Keys()
    {
        var data = new TheoryData<string>();
        foreach (var p in ProgramCatalog.All)
            data.Add(p.Key);
        return data;
    }

    [Fact]
    public void Has_all_groups_with_unique_keys()
    {
        ProgramCatalog.All.Select(p => p.Key).Should().OnlyHaveUniqueItems();
        ProgramCatalog.All.Select(p => p.Profile.Category).Distinct()
            .Should().BeEquivalentTo(Enum.GetValues<ProgramCategory>());
        ProgramCatalog.All.Should().HaveCount(15);
    }

    [Theory]
    [MemberData(nameof(Keys))]
    public void Builds_a_valid_plan_for_its_weeks_and_days(string key)
    {
        var program = ProgramCatalog.Find(key)!;

        var content = program.ToContent(NoLibrary);

        PlanDocumentRules.Validate(content.Document).IsSuccess.Should().BeTrue();
        content.DurationWeeks.Should().Be(program.Profile.Weeks);
        content.DaysPerWeek.Should().Be(program.DaysPerWeek);
        content.NameAr.Should().NotBeNullOrWhiteSpace();
        content.Document.Sequence().Should().OnlyContain(d => !string.IsNullOrWhiteSpace(d.TitleAr));
        content.Document.Sequence().SelectMany(d => d.Exercises).Should().OnlyContain(e => !string.IsNullOrWhiteSpace(e.NameAr));
        content.Document.Sequence().Select(d => d.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Links_exercises_to_the_library_by_name()
    {
        var squatId = Guid.NewGuid();
        var library = new Dictionary<string, (Guid, string?)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Barbell Back Squat"] = (squatId, "سكوات خلفي بالبار")
        };

        var content = ProgramCatalog.Find("strength_5x5_3d")!.ToContent(library);

        var squat = content.Document.Sequence().First().Exercises.First();
        squat.ExerciseId.Should().Be(squatId);
        squat.NameAr.Should().Be("سكوات خلفي بالبار", "the library's Arabic name wins");
    }

    [Fact]
    public void Five_by_five_alternates_A_and_B()
    {
        var titles = ProgramCatalog.Find("strength_5x5_3d")!.ToContent(NoLibrary).Document.Sequence().Take(6).Select(d => d.Title);

        titles.Should().Equal("Workout A", "Workout B", "Workout A", "Workout B", "Workout A", "Workout B");
    }

    [Fact]
    public void Walk_to_run_runs_longer_every_week_up_to_twenty_minutes()
    {
        var weeks = ProgramCatalog.Find("walk_to_run_8w")!.ToContent(NoLibrary).Document.Weeks;

        var runs = weeks.Select(w => w.Days[0].Exercises.Single(e => e.Name == "Treadmill Running")).ToList();
        runs.Select(r => r.Seconds!.Value * r.Sets).Should().BeInAscendingOrder();
        runs.Last().Seconds.Should().Be(1200);
    }

    [Fact]
    public void Unknown_key_is_not_found() => ProgramCatalog.Find("nope").Should().BeNull();
}
