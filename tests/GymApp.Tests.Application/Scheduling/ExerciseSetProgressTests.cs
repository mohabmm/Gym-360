using FluentAssertions;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Scheduling.Application.Commands.LogSessionExercises;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Application.Queries.GetExerciseProgress;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Tests.Application.Scheduling;

/// <summary>pt.exercise_progress: the board's numbers when the coach logs every set (weight per set, timed holds).</summary>
public sealed class ExerciseSetProgressTests
{
    private static readonly Guid Member = Guid.NewGuid();

    private static ExerciseSetEntry Lift(int reps, decimal? kg) => new(reps, kg, null);

    private static ExerciseSetEntry Hold(int seconds, decimal? kg = null) => new(null, kg, seconds);

    private static ExerciseLogDetails Logged(string name, ExerciseMeasure measure, ExerciseSetEntry[] sets, ExerciseTarget? target = null) => new()
    {
        ExerciseName = name,
        OrderIndex = 0,
        Sets = 0,
        Reps = string.Empty,
        Measure = measure,
        SetEntries = sets,
        Target = target
    };

    private static Session On(int day, ExerciseLogDetails log)
    {
        var session = Session.LogCompleted(
            new SessionParticipants(Guid.NewGuid(), Guid.NewGuid(), Member),
            new SessionSchedule(new DateOnly(2026, 11, day), new TimeOnly(18, 0), 60),
            notes: null);
        session.AddExerciseLog(log);
        return session;
    }

    private static ExerciseHistoryDto Board(params Session[] sessions) =>
        ExerciseProgressCalculator.Build(sessions, new PtProgressScope(new HashSet<Guid>(), 0, HasPaidPackage: true))
            .Exercises.Single();

    [Fact]
    public void Pyramid_uses_each_sets_weight_for_volume_and_one_rep_max()
    {
        var entry = Board(On(1, Logged("Leg press", ExerciseMeasure.Reps, [Lift(12, 60m), Lift(10, 70m), Lift(8, 80m)])))
            .Entries.Single();

        // 12×60 + 10×70 + 8×80 = 2060; best Epley: 80 × (1 + 8/30) = 101.33 → 101.5
        new { entry.Weight, entry.Volume, entry.EstimatedOneRepMax, entry.TotalReps }
            .Should().BeEquivalentTo(new { Weight = (decimal?)80m, Volume = (decimal?)2060m, EstimatedOneRepMax = (decimal?)101.5m, TotalReps = 30 });
        entry.SetEntries.Should().HaveCount(3);
    }

    [Fact]
    public void Bodyweight_sets_have_no_volume()
    {
        var entry = Board(On(1, Logged("Push-up", ExerciseMeasure.Reps, [Lift(20, null), Lift(15, null)]))).Entries.Single();

        new { entry.Weight, entry.Volume, entry.EstimatedOneRepMax, entry.TotalReps }
            .Should().BeEquivalentTo(new { Weight = (decimal?)null, Volume = (decimal?)null, EstimatedOneRepMax = (decimal?)null, TotalReps = 35 });
    }

    [Fact]
    public void Plank_reports_seconds_and_no_weight_numbers()
    {
        var entry = Board(On(1, Logged("Plank", ExerciseMeasure.Time, [Hold(45), Hold(40), Hold(30)]))).Entries.Single();

        new { entry.Measure, entry.TotalSeconds, entry.BestSeconds, entry.Weight, entry.Volume, entry.EstimatedOneRepMax }
            .Should().BeEquivalentTo(new
            {
                Measure = ExerciseMeasure.Time,
                TotalSeconds = (int?)115,
                BestSeconds = (int?)45,
                Weight = (decimal?)null,
                Volume = (decimal?)null,
                EstimatedOneRepMax = (decimal?)null
            });
    }

    [Fact]
    public void Longer_plank_is_a_personal_best_and_meets_the_seconds_target()
    {
        var plank = Board(
            On(1, Logged("Plank", ExerciseMeasure.Time, [Hold(40), Hold(35)], new ExerciseTarget(null, null, null, Seconds: 45))),
            On(3, Logged("Plank", ExerciseMeasure.Time, [Hold(60), Hold(50)])));

        new { plank.Trend.LatestIsPersonalBest, plank.Trend.HitPreviousTarget, plank.Trend.LatestBestSeconds, plank.Trend.BestSecondsEver, plank.Trend.SecondsChangePercent }
            .Should().BeEquivalentTo(new
            {
                LatestIsPersonalBest = true,
                HitPreviousTarget = (bool?)true,
                LatestBestSeconds = (int?)60,
                BestSecondsEver = (int?)60,
                SecondsChangePercent = (decimal?)50m
            });
    }

    [Fact]
    public void Seconds_target_is_missed_when_one_set_falls_short()
    {
        var plank = Board(
            On(1, Logged("Plank", ExerciseMeasure.Time, [Hold(40)], new ExerciseTarget(2, null, null, Seconds: 45))),
            On(3, Logged("Plank", ExerciseMeasure.Time, [Hold(50), Hold(30)])));

        plank.Trend.HitPreviousTarget.Should().BeFalse();
        plank.Trend.LatestIsPersonalBest.Should().BeTrue();
    }

    [Fact]
    public void Heavier_top_set_is_a_personal_best_and_meets_the_weight_target()
    {
        var press = Board(
            On(1, Logged("Leg press", ExerciseMeasure.Reps, [Lift(12, 60m), Lift(10, 70m)], new ExerciseTarget(null, "10", 75m))),
            On(3, Logged("Leg press", ExerciseMeasure.Reps, [Lift(12, 60m), Lift(10, 75m)])));

        press.Trend.LatestIsPersonalBest.Should().BeTrue();
        press.Trend.HitPreviousTarget.Should().BeTrue();
    }

    [Fact]
    public void Switching_an_exercise_to_time_compares_only_timed_entries()
    {
        var plank = Board(
            On(1, new ExerciseLogDetails { ExerciseName = "Plank", OrderIndex = 0, Sets = 3, Reps = "60s" }),
            On(3, Logged("Plank", ExerciseMeasure.Time, [Hold(30)])));

        plank.Trend.LatestIsPersonalBest.Should().BeFalse("the first timed entry has nothing to beat");
        plank.Trend.BestSecondsEver.Should().Be(30, "the old reps-style plank row is not read as seconds");
    }

    public sealed record ValidationCase(string Name, ExerciseMeasure Measure, ExerciseSetEntry[] Sets, bool Valid);

    public static TheoryData<ValidationCase> ValidationCases => new()
    {
        new("pyramid", ExerciseMeasure.Reps, [Lift(12, 60m), Lift(10, 70m)], true),
        new("reps set without reps", ExerciseMeasure.Reps, [Lift(12, 60m), Hold(30)], false),
        new("plank", ExerciseMeasure.Time, [Hold(45), Hold(40, 10m)], true),
        new("timed set without seconds", ExerciseMeasure.Time, [Lift(12, null)], false),
        new("hold too long", ExerciseMeasure.Time, [Hold(ExerciseSetEntry.MaxSeconds + 1)], false),
        new("negative kg", ExerciseMeasure.Reps, [Lift(10, -5m)], false),
        new("too many sets", ExerciseMeasure.Reps, Enumerable.Repeat(Lift(10, 20m), ExerciseSetEntry.MaxSets + 1).ToArray(), false),
    };

    [Theory]
    [MemberData(nameof(ValidationCases))]
    public void Set_entries_are_validated_by_measure(ValidationCase c)
    {
        var item = new ExerciseLogItem(null, "Exercise", null, 0, 0, string.Empty, null, null, null, c.Measure, c.Sets);
        var command = new LogSessionExercisesCommand(Guid.NewGuid(), Guid.NewGuid(), [item]);

        new LogSessionExercisesCommandValidator().Validate(command).IsValid.Should().Be(c.Valid, c.Name);
    }

    [Fact]
    public void Legacy_payload_without_sets_still_needs_reps()
    {
        var item = new ExerciseLogItem(null, "Squat", null, 0, 3, string.Empty, 80m, null);

        new LogSessionExercisesCommandValidator()
            .Validate(new LogSessionExercisesCommand(Guid.NewGuid(), Guid.NewGuid(), [item]))
            .IsValid.Should().BeFalse();
    }
}
