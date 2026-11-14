using FluentAssertions;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Scheduling.Application.Queries.GetExerciseProgress;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Tests.Application.Scheduling;

/// <summary>pt.exercise_progress: how the member's board is built from the coach's logs.</summary>
public sealed class ExerciseProgressCalculatorTests
{
    private static readonly Guid Member = Guid.NewGuid();
    private static readonly Guid Coach = Guid.NewGuid();
    private static readonly Guid Gym = Guid.NewGuid();

    private static readonly ExerciseLogDetails BenchPress = Lift("Bench press");
    private static readonly ExerciseLogDetails Row = Lift("Barbell row");

    /// <summary>When a session happened (day of November 2026) and the coach's day label.</summary>
    private sealed record On(int Day, string? Label = null);

    public sealed record TargetCase(string Reps, decimal Weight, bool Hit);

    public sealed record RepsCase(string Reps, int Sets, int[] Expected);

    private static ExerciseLogDetails Lift(string name) => new() { ExerciseName = name, OrderIndex = 0, Sets = 3, Reps = "10" };

    private static Session Logged(On on, params ExerciseLogDetails[] logs)
    {
        var session = Session.LogCompleted(
            new SessionParticipants(Gym, Coach, Member),
            new SessionSchedule(new DateOnly(2026, 11, on.Day), new TimeOnly(18, 0), 60),
            notes: null);
        session.SetDayLabel(on.Label);
        foreach (var log in logs)
            session.AddExerciseLog(log);
        return session;
    }

    private static PtProgressScope TrialOf(params Session[] sessions) =>
        new(sessions.Select(s => s.Id).ToHashSet(), 20, HasPaidPackage: false);

    private static ExerciseProgressDtoAccess Board(params Session[] sessions) =>
        new(ExerciseProgressCalculator.Build(sessions, TrialOf(sessions)));

    [Fact]
    public void Empty_history_gives_an_empty_trial_board()
    {
        var board = ExerciseProgressCalculator.Build(Array.Empty<Session>(), PtProgressScope.None);

        board.Exercises.Should().BeEmpty();
        board.Summary.IsTrialView.Should().BeTrue();
        board.Summary.FirstSessionDate.Should().BeNull();
    }

    [Fact]
    public void Sessions_are_grouped_by_day_label_with_their_exercises()
    {
        var push = Logged(new On(1, "Push Day"), BenchPress with { Weight = 60m }, Lift("Shoulder press") with { Weight = 30m });
        var pull = Logged(new On(3, "Pull Day"), Row with { Weight = 50m });
        var push2 = Logged(new On(8, "Push Day"), BenchPress with { Weight = 62.5m });

        var board = Board(push, pull, push2).Dto;

        board.Days.Select(d => d.Label).Should().Equal("Push Day", "Pull Day");
        board.Days[0].ExerciseKeys.Should().Equal("bench press", "shoulder press");
        board.Days[0].LastTrained.Should().Be(new DateOnly(2026, 11, 8));
    }

    [Fact]
    public void Summary_counts_logged_and_trial_sessions()
    {
        var summary = Board(Logged(new On(1), BenchPress), Logged(new On(2), BenchPress)).Dto.Summary;

        summary.SessionsLogged.Should().Be(2);
        summary.TrialSessionsUsed.Should().Be(2);
        summary.TrialSessionsTotal.Should().Be(20);
    }

    [Fact]
    public void Same_exercise_matches_by_name_ignoring_case_and_spacing()
    {
        var board = Board(Logged(new On(1), Lift("Bench  Press")), Logged(new On(4), Lift("bench press"))).Dto;

        board.Exercises.Should().ContainSingle().Which.Entries.Select(e => e.SessionNumber).Should().Equal(1, 2);
        board.Days.Should().ContainSingle(d => d.Label == null);
    }

    [Fact]
    public void Library_exercise_id_wins_over_the_name()
    {
        var id = Guid.NewGuid();

        var board = Board(
            Logged(new On(1), Lift("Squat") with { ExerciseId = id }),
            Logged(new On(4), Lift("Back squat") with { ExerciseId = id })).Dto;

        board.Exercises.Should().ContainSingle(e => e.Key == id.ToString());
    }

    [Fact]
    public void Entry_reports_total_reps_volume_and_one_rep_max()
    {
        var entry = Board(Logged(new On(1), BenchPress with { Reps = "10,8,8", Weight = 60m })).Single.Entries[0];

        entry.TotalReps.Should().Be(26);
        entry.Volume.Should().Be(1560m);
        entry.EstimatedOneRepMax.Should().Be(80m); // 60 × (1 + 10/30)
    }

    [Fact]
    public void Trend_reports_change_and_personal_best()
    {
        var trend = Board(
            Logged(new On(1), BenchPress with { Reps = "10,8,8", Weight = 60m }),
            Logged(new On(4), BenchPress with { Reps = "10,10,8", Weight = 67.5m })).Single.Trend;

        trend.WeightChangePercent.Should().Be(12.5m);
        trend.LatestVolume.Should().Be(1890m);
        trend.LatestIsPersonalBest.Should().BeTrue();
    }

    [Fact]
    public void Same_weight_with_more_reps_is_a_personal_best_and_fewer_is_not()
    {
        var s1 = Logged(new On(1), Row with { Reps = "8", Weight = 50m });
        var s2 = Logged(new On(2), Row with { Reps = "10", Weight = 50m });
        var s3 = Logged(new On(3), Row with { Reps = "9", Weight = 50m });

        Board(s1, s2).Single.Trend.LatestIsPersonalBest.Should().BeTrue();
        Board(s1, s2, s3).Single.Trend.LatestIsPersonalBest.Should().BeFalse();
    }

    [Fact]
    public void Single_bodyweight_entry_has_no_volume_or_change()
    {
        var pushUp = Board(Logged(new On(1), Lift("Push-up") with { Reps = "15" })).Single;

        pushUp.Entries[0].Volume.Should().BeNull();
        pushUp.Trend.WeightChangePercent.Should().BeNull();
        pushUp.Trend.HitPreviousTarget.Should().BeNull();
    }

    public static TheoryData<TargetCase> TargetCases => new()
    {
        new TargetCase("10,10,10", 62.5m, true),
        new TargetCase("10,10,9", 62.5m, false),
        new TargetCase("10,10,10", 60m, false),
    };

    [Theory]
    [MemberData(nameof(TargetCases))]
    public void Latest_session_is_checked_against_the_previous_target(TargetCase c)
    {
        var bench = Board(
            Logged(new On(1), BenchPress with { Reps = "10,8,8", Weight = 60m, Target = new ExerciseTarget(3, "10", 62.5m) }),
            Logged(new On(4), BenchPress with { Reps = c.Reps, Weight = c.Weight })).Single;

        bench.Trend.HitPreviousTarget.Should().Be(c.Hit);
        bench.NextTarget.Should().BeNull();
    }

    [Fact]
    public void Paid_member_sees_trial_and_paid_sessions_with_trial_ones_marked()
    {
        var trial = Logged(new On(1), Lift("Squat") with { Weight = 80m });
        var paid = Logged(new On(20), Lift("Squat") with { Weight = 90m });
        var scope = new PtProgressScope(new HashSet<Guid> { trial.Id }, 3, HasPaidPackage: true);

        var board = ExerciseProgressCalculator.Build(new[] { trial, paid }, scope);

        board.Summary.IsTrialView.Should().BeFalse();
        board.Exercises.Single().Entries.Select(e => e.IsTrialSession).Should().Equal(true, false);
    }

    public static TheoryData<RepsCase> RepsCases => new()
    {
        new RepsCase("10", 3, new[] { 10, 10, 10 }),
        new RepsCase("10,8,6", 3, new[] { 10, 8, 6 }),
        new RepsCase("8-12", 2, new[] { 8, 8 }),
        new RepsCase("abc", 3, Array.Empty<int>()),
    };

    [Theory]
    [MemberData(nameof(RepsCases))]
    public void Reps_text_is_read_per_set(RepsCase c)
    {
        RepsParser.PerSet(c.Reps, c.Sets).Should().Equal(c.Expected);
    }

    private sealed record ExerciseProgressDtoAccess(Modules.Scheduling.Application.DTOs.ExerciseProgressDto Dto)
    {
        public Modules.Scheduling.Application.DTOs.ExerciseHistoryDto Single => Dto.Exercises.Single();
    }
}
