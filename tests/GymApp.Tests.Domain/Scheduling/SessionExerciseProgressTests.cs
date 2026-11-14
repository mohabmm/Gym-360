using FluentAssertions;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Scheduling;

/// <summary>pt.exercise_progress: day label and next-session target on the coach's exercise log.</summary>
public sealed class SessionExerciseProgressTests
{
    private static Session Completed() =>
        Session.LogCompleted(
            new SessionParticipants(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            new SessionSchedule(new DateOnly(2026, 11, 12), new TimeOnly(18, 0), 60),
            notes: null);

    private static ExerciseLogDetails Bench(ExerciseTarget? target = null) => new()
    {
        ExerciseName = "Bench press",
        OrderIndex = 0,
        Sets = 3,
        Reps = "10,8,8",
        Weight = 60m,
        Notes = "Elbows in",
        Target = target
    };

    [Fact]
    public void Day_label_is_trimmed_and_blank_clears_it()
    {
        var session = Completed();

        session.SetDayLabel("  Push Day ").IsSuccess.Should().BeTrue();
        session.DayLabel.Should().Be("Push Day");

        session.SetDayLabel("   ").IsSuccess.Should().BeTrue();
        session.DayLabel.Should().BeNull();
    }

    [Fact]
    public void Day_label_longer_than_the_limit_is_refused()
    {
        var result = Completed().SetDayLabel(new string('x', Session.MaxDayLabelLength + 1));

        result.Error.Should().Be(SchedulingErrors.DayLabelTooLong);
    }

    [Fact]
    public void Day_label_needs_a_completed_session()
    {
        var booked = Session.Create(
            new SessionParticipants(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            new SessionSchedule(new DateOnly(2026, 11, 20), new TimeOnly(18, 0), 60),
            Modules.Scheduling.Domain.Enums.SessionType.PersonalTraining);

        booked.SetDayLabel("Legs").Error.Should().Be(SchedulingErrors.ExerciseLogRequiresCompletedSession);
    }

    [Fact]
    public void Target_is_stored_on_create_and_replaced_on_update()
    {
        var session = Completed();
        var log = session.AddExerciseLog(Bench(new ExerciseTarget(3, "10", 62.5m))).Value;

        log.TargetSets.Should().Be(3);
        log.TargetReps.Should().Be("10");
        log.TargetWeight.Should().Be(62.5m);

        session.UpdateExerciseLog(log.Id, Bench()).IsSuccess.Should().BeTrue();

        log.TargetSets.Should().BeNull();
        log.TargetReps.Should().BeNull();
        log.TargetWeight.Should().BeNull();
    }

    [Fact]
    public void Empty_target_reports_itself_empty()
    {
        new ExerciseTarget(null, " ", null).IsEmpty.Should().BeTrue();
        new ExerciseTarget(null, null, 50m).IsEmpty.Should().BeFalse();
    }
}
