using FluentAssertions;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.ValueObjects;
using Xunit;
using static GymApp.Tests.Domain.Training.Plans.PlanSamples;

namespace GymApp.Tests.Domain.Training.Plans;

public class PlanDocumentTests
{
    [Fact]
    public void Normalizing_gives_every_day_and_exercise_a_unique_key()
    {
        var doc = FullBodyWeek().CopyFirstWeekToAll(4);

        var keys = doc.Sequence().SelectMany(d => d.Exercises.Select(e => e.Key).Prepend(d.Key)).ToList();

        keys.Should().OnlyHaveUniqueItems().And.NotContain(string.Empty);
    }

    [Fact]
    public void Normalizing_keeps_existing_keys_so_completions_still_match_after_an_edit()
    {
        var doc = FullBodyWeek().Normalized();

        doc.Normalized().Sequence().Select(d => d.Key).Should().Equal(doc.Sequence().Select(d => d.Key));
    }

    [Fact]
    public void Copy_week_one_to_all_weeks_repeats_the_days_in_order()
    {
        var doc = FullBodyWeek().CopyFirstWeekToAll(4);

        doc.DurationWeeks.Should().Be(4);
        doc.Weeks.Select(w => w.Week).Should().Equal(1, 2, 3, 4);
        doc.Sequence().Select(d => d.Title).Should().Equal(Enumerable.Repeat(new[] { "Day A", "Day B", "Day C" }, 4).SelectMany(x => x));
    }

    [Fact]
    public void Sequence_flattens_weeks_in_week_order()
    {
        var doc = PlanDocument.Of(new[]
        {
            new PlanWeek(2, new[] { Day("W2", Exercise("Row")) }),
            new PlanWeek(1, new[] { Day("W1", Exercise("Squat")) }),
        });

        doc.Sequence().Select(d => d.Title).Should().Equal("W1", "W2");
    }

    [Theory]
    [InlineData("10", 30)]   // reps and time
    [InlineData(null, null)] // neither
    public void An_exercise_needs_either_reps_or_a_time(string? reps, int? seconds)
    {
        var result = PlanContent.Create("P", null, PlanGoal.FatLoss, null,
            OneWeek(Day("A", Exercise("Squat", 3, reps, seconds))));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Training.InvalidPlan");
    }

    [Fact]
    public void A_timed_exercise_is_valid()
    {
        PlanContent.Create("P", null, PlanGoal.FatLoss, null,
            OneWeek(Day("A", Exercise("Plank", 3, reps: null, seconds: 45)))).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Free_text_goal_needs_a_description()
    {
        PlanContent.Create("P", null, PlanGoal.Other, "  ", FullBodyWeek()).IsFailure.Should().BeTrue();
        PlanContent.Create("P", null, PlanGoal.Other, "Football pre-season", FullBodyWeek())
            .Value.GoalText.Should().Be("Football pre-season");
    }

    [Fact]
    public void Video_link_must_be_a_web_address()
    {
        var bad = Exercise("Squat") with { VideoUrl = "javascript:alert(1)" };

        PlanContent.Create("P", null, PlanGoal.FatLoss, null, OneWeek(Day("A", bad))).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void A_day_without_exercises_is_rejected()
    {
        PlanContent.Create("P", null, PlanGoal.FatLoss, null, OneWeek(Day("Empty"))).IsFailure.Should().BeTrue();
    }
}
