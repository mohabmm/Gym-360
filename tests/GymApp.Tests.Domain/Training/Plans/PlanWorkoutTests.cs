using FluentAssertions;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.ValueObjects;
using Xunit;
using static GymApp.Tests.Domain.Training.Plans.PlanSamples;

namespace GymApp.Tests.Domain.Training.Plans;

public class PlanWorkoutTests
{
    private static readonly DateOnly Today = new(2026, 11, 25); // a Wednesday

    private static TrainingPlan TwoWeekPlan() => TrainingPlan.Assign(
        new PlanAssignment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 21), null),
        Content(FullBodyWeek().CopyFirstWeekToAll(2)));

    private static PlanSlot SlotAt(TrainingPlan plan, int index) => PlanCursor.Slots(plan.Document)[index];

    private static PlanWorkoutCompletion Done(TrainingPlan plan, int index, DateOnly on, params CompletedSet[] sets) =>
        PlanWorkoutCompletion.Record(plan, SlotAt(plan, index), new PlanWorkoutLog
        {
            ClientId = Guid.NewGuid(),
            CompletedAt = on.ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc),
            CompletedOn = on,
            Sets = sets
        });

    /// <summary>The cursor after doing these places in the plan, each some days ago.</summary>
    private static PlanCursor CursorAfter(params (int Index, int DaysAgo)[] done)
    {
        var plan = TwoWeekPlan();
        var marks = done.Select(d => Done(plan, d.Index, Today.AddDays(-d.DaysAgo)).Mark).ToList();
        return PlanCursor.For(plan.Document, marks, Today);
    }

    private static CompletedSet Set(string name, decimal? kg, bool done = true, Guid? exerciseId = null) =>
        new() { ExerciseKey = "e-" + name, ExerciseId = exerciseId, ExerciseName = name, SetNumber = 1, Reps = 10, WeightKg = kg, Done = done };

    [Fact]
    public void Slots_run_week_by_week_in_order()
    {
        var slots = PlanCursor.Slots(TwoWeekPlan().Document);

        slots.Should().HaveCount(6);
        slots.Select(s => s.Week).Should().Equal(1, 1, 1, 2, 2, 2);
        slots.Select(s => s.Index).Should().Equal(0, 1, 2, 3, 4, 5);
        slots.Select(s => s.Day.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void With_nothing_done_the_next_workout_is_week_one_day_one()
    {
        var cursor = CursorAfter();

        cursor.Next!.Index.Should().Be(0);
        cursor.LastDoneIndex.Should().Be(-1);
        cursor.DoneToday.Should().BeFalse();
        cursor.Finished.Should().BeFalse();
    }

    [Fact]
    public void A_missed_calendar_day_does_not_skip_a_workout_the_next_one_is_after_the_last_done()
    {
        var cursor = CursorAfter((0, 6), (1, 4));

        cursor.Next!.Index.Should().Be(2);
        cursor.Next.Day.Title.Should().Be("Day C");
        cursor.DoneKeys.Should().HaveCount(2);
    }

    [Fact]
    public void Doing_a_later_day_moves_on_from_there_and_repeating_an_earlier_one_does_not_go_back()
    {
        var cursor = CursorAfter((3, 2), (0, 0));

        cursor.Next!.Index.Should().Be(4);
        cursor.Next.Week.Should().Be(2);
        cursor.DoneToday.Should().BeTrue();
    }

    [Fact]
    public void After_the_last_day_the_plan_is_finished()
    {
        var cursor = CursorAfter((5, 0));

        cursor.Next.Should().BeNull();
        cursor.Finished.Should().BeTrue();
    }

    [Fact]
    public void A_day_the_coach_removed_keeps_its_place_so_the_trainee_does_not_restart()
    {
        var plan = TwoWeekPlan();
        var removed = new CompletionMark("d-gone", SequenceIndex: 2, Today.AddDays(-1));

        var cursor = PlanCursor.For(plan.Document, new[] { removed }, Today);

        cursor.Next!.Index.Should().Be(3);
        cursor.DoneKeys.Should().BeEmpty();
    }

    [Fact]
    public void The_week_starts_on_saturday()
    {
        PlanCalendar.WeekStart(new DateOnly(2026, 11, 21)).Should().Be(new DateOnly(2026, 11, 21)); // Saturday
        PlanCalendar.WeekStart(new DateOnly(2026, 11, 27)).Should().Be(new DateOnly(2026, 11, 21)); // Friday
        PlanCalendar.WeekStart(new DateOnly(2026, 11, 28)).Should().Be(new DateOnly(2026, 11, 28));
    }

    [Fact]
    public void Week_and_month_counts_skip_voided_workouts()
    {
        var plan = TwoWeekPlan();
        var lastFriday = Done(plan, 0, new DateOnly(2026, 11, 20));
        var saturday = Done(plan, 1, new DateOnly(2026, 11, 21));
        var voided = Done(plan, 2, Today);
        voided.Void();

        var stats = PlanProgress.Stats(new[] { lastFriday, saturday, voided }, Today, plan.DaysPerWeek);

        stats.ThisWeek.Should().Be(1);
        stats.ThisMonth.Should().Be(2);
        stats.WeekTarget.Should().Be(3);
        stats.WeekStart.Should().Be(new DateOnly(2026, 11, 21));
        stats.LastWorkoutOn.Should().Be(new DateOnly(2026, 11, 21));
    }

    [Fact]
    public void Last_weights_prefill_each_plan_exercise_with_the_latest_weight_done()
    {
        var plan = TwoWeekPlan();
        var older = Done(plan, 0, Today.AddDays(-7), Set("Squat", 50));
        var newer = Done(plan, 3, Today.AddDays(-2), Set("squat", 55), Set("Bench press", 40, done: false));

        var weights = PlanProgress.LastWeights(plan.Document, new[] { newer, older });

        var squatKeys = plan.Document.Sequence().SelectMany(d => d.Exercises).Where(e => e.Name == "Squat").Select(e => e.Key);
        weights.Keys.Should().BeEquivalentTo(squatKeys);
        weights.Values.Should().AllBeEquivalentTo(55m);
    }

    [Fact]
    public void Trends_keep_the_heaviest_set_per_day_oldest_first()
    {
        var plan = TwoWeekPlan();
        var first = Done(plan, 0, Today.AddDays(-7), Set("Squat", 50), Set("Squat", 52.5m));
        var second = Done(plan, 3, Today, Set("Squat", 57.5m), Set("Plank", null));

        var trends = PlanProgress.Trends(new[] { second, first });

        var squat = trends.Should().ContainSingle().Subject;
        squat.Name.Should().Be("Squat");
        squat.Points.Select(p => p.WeightKg).Should().Equal(52.5m, 57.5m);
    }

    [Fact]
    public void A_completion_copies_the_day_so_a_later_edit_cannot_rewrite_history()
    {
        var plan = TwoWeekPlan();
        var done = Done(plan, 4, Today);

        done.DayTitle.Should().Be("Day B");
        done.Week.Should().Be(2);
        done.SequenceIndex.Should().Be(4);
        done.PlanVersion.Should().Be(1);
        done.TraineeId.Should().Be(plan.TraineeId);
    }

    [Theory]
    [InlineData(-1, null, null)]
    [InlineData(null, 501, null)]
    [InlineData(null, null, 501)]
    public void Impossible_numbers_are_rejected(int? reps, int? kg, int? reps2)
    {
        var set = Set("Squat", kg) with { Reps = reps ?? reps2 };
        var log = new PlanWorkoutLog { ClientId = Guid.NewGuid(), CompletedAt = DateTime.UtcNow, CompletedOn = Today, Sets = new[] { set } };

        PlanWorkoutLogRules.Validate(log).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void A_workout_without_a_device_id_is_rejected()
    {
        var log = new PlanWorkoutLog { ClientId = Guid.Empty, CompletedAt = DateTime.UtcNow, CompletedOn = Today };

        PlanWorkoutLogRules.Validate(log).Error.Code.Should().Be("Training.InvalidPlanWorkout");
    }
}
