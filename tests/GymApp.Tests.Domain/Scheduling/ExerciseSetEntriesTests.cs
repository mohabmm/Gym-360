using FluentAssertions;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Scheduling;

/// <summary>pt.exercise_progress: per-set logging (weight per set, timed sets) and the legacy summary it derives.</summary>
public sealed class ExerciseSetEntriesTests
{
    private static Session Completed() =>
        Session.LogCompleted(
            new SessionParticipants(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            new SessionSchedule(new DateOnly(2026, 11, 21), new TimeOnly(18, 0), 60),
            notes: null);

    private static ExerciseLogDetails WithSets(ExerciseMeasure measure, params ExerciseSetEntry[] sets) => new()
    {
        ExerciseName = "Exercise",
        OrderIndex = 0,
        Sets = 0,
        Reps = string.Empty,
        Measure = measure,
        SetEntries = sets
    };

    public sealed record SummaryCase(ExerciseMeasure Measure, ExerciseSetEntry[] Sets, int ExpectedSets, string ExpectedReps, decimal? ExpectedWeight);

    public static TheoryData<SummaryCase> SummaryCases => new()
    {
        // Pyramid: reps per set, the heaviest set is the working weight.
        new(ExerciseMeasure.Reps, [new(12, 60m, null), new(10, 70m, null), new(8, 80m, null)], 3, "12,10,8", 80m),
        // Same reps every set collapse to one value, as before.
        new(ExerciseMeasure.Reps, [new(12, 60m, null), new(12, 60m, null), new(12, 60m, null)], 3, "12", 60m),
        // Bodyweight: no kg.
        new(ExerciseMeasure.Reps, [new(15, null, null), new(12, null, null)], 2, "15,12", null),
        // Plank: seconds, marked so older apps read "45s".
        new(ExerciseMeasure.Time, [new(null, null, 45), new(null, null, 40), new(null, null, 30)], 3, "45s,40s,30s", null),
        // Weighted plank keeps the plate.
        new(ExerciseMeasure.Time, [new(null, 10m, 60), new(null, 10m, 60)], 2, "60s", 10m),
    };

    [Theory]
    [MemberData(nameof(SummaryCases))]
    public void Per_set_entries_derive_the_legacy_summary(SummaryCase c)
    {
        var log = Completed().AddExerciseLog(WithSets(c.Measure, c.Sets)).Value;

        new { log.Sets, log.Reps, log.Weight, log.Measure }
            .Should().BeEquivalentTo(new { Sets = c.ExpectedSets, Reps = c.ExpectedReps, Weight = c.ExpectedWeight, c.Measure });
        log.SetEntries.Should().Equal(c.Sets);
    }

    [Fact]
    public void Without_set_entries_the_log_is_stored_as_sent()
    {
        var details = new ExerciseLogDetails { ExerciseName = "Squat", OrderIndex = 0, Sets = 4, Reps = "10,8,8,6", Weight = 100m };

        var log = Completed().AddExerciseLog(details).Value;

        new { log.Sets, log.Reps, log.Weight, log.Measure }
            .Should().BeEquivalentTo(new { Sets = 4, Reps = "10,8,8,6", Weight = (decimal?)100m, Measure = ExerciseMeasure.Reps });
        log.SetEntries.Should().BeEmpty();
    }

    [Fact]
    public void Updating_replaces_the_sets_and_the_target_seconds()
    {
        var session = Completed();
        var log = session.AddExerciseLog(WithSets(ExerciseMeasure.Reps, new ExerciseSetEntry(10, 50m, null))).Value;

        var update = WithSets(ExerciseMeasure.Time, new ExerciseSetEntry(null, null, 50), new ExerciseSetEntry(null, null, 45))
            with { Target = new ExerciseTarget(null, null, null, Seconds: 60) };
        session.UpdateExerciseLog(log.Id, update).IsSuccess.Should().BeTrue();

        new { log.Measure, log.Reps, log.TargetSeconds, Count = log.SetEntries.Count }
            .Should().BeEquivalentTo(new { Measure = ExerciseMeasure.Time, Reps = "50s,45s", TargetSeconds = (int?)60, Count = 2 });
    }

    [Fact]
    public void A_target_with_only_seconds_is_not_empty()
    {
        new ExerciseTarget(null, null, null, Seconds: 60).IsEmpty.Should().BeFalse();
    }
}
