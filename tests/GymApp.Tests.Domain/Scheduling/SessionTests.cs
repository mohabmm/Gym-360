using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Events;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Scheduling;

public sealed class SessionTests
{
    private static readonly Guid GymId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();

    // The default session is scheduled 2025-06-15 10:00; this clock is just
    // before it so Confirm() passes its "not in the past" guard.
    private static readonly DateTime ClockBeforeSchedule =
        new(2025, 6, 14, 0, 0, 0, DateTimeKind.Utc);

    private static Session CreateDefaultSession()
    {
        return Session.Create(
            new SessionParticipants(GymId, TrainerId, MemberId),
            new SessionSchedule(new DateOnly(2025, 6, 15), new TimeOnly(10, 0), 60),
            SessionType.PersonalTraining,
            "Upper body");
    }

    private static readonly TrainerLogDetails ValidTrainerLog = new()
    {
        ExercisesPerformed = "Exercises",
        ClientEffortRating = 3,
        AttendanceConfirmed = true
    };

    private static readonly MemberLogDetails ValidMemberLog = new()
    {
        Rating = 5,
        PerceivedExertion = 7,
        Satisfaction = 5
    };

    private static Session CreateConfirmedSession()
    {
        var session = CreateDefaultSession();
        session.Confirm(ClockBeforeSchedule);
        session.ClearDomainEvents();
        return session;
    }

    private static Session CreateInProgressSession()
    {
        var session = CreateConfirmedSession();
        session.StartSession(DateTime.UtcNow, 15, true);
        return session;
    }

    private static Session CreateCompletedSession()
    {
        var session = CreateInProgressSession();
        session.Complete();
        session.ClearDomainEvents();
        return session;
    }

    // --- Creation ---

    [Fact]
    public void Create_ValidInput_ShouldSetPropertiesAndRaiseEvent()
    {
        var session = Session.Create(
            new SessionParticipants(GymId, TrainerId, MemberId),
            new SessionSchedule(new DateOnly(2025, 6, 15), new TimeOnly(10, 0), 60),
            SessionType.PersonalTraining,
            "Focus");

        session.Id.Should().NotBeEmpty();
        session.GymId.Should().Be(GymId);
        session.TrainerId.Should().Be(TrainerId);
        session.MemberId.Should().Be(MemberId);
        session.Status.Should().Be(SessionStatus.Pending);
        session.DurationMinutes.Should().Be(60);
        session.Focus.Should().Be("Focus");
        session.IsRecurring.Should().BeFalse();

        session.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<SessionBookedDomainEvent>();
    }

    [Fact]
    public void Create_Recurring_ShouldSetRecurrenceFields()
    {
        var session = Session.CreateRecurring(
            new SessionParticipants(GymId, TrainerId, MemberId),
            new SessionSchedule(new DateOnly(2025, 6, 15), new TimeOnly(10, 0), 60),
            SessionType.PersonalTraining,
            "FREQ=WEEKLY");

        session.IsRecurring.Should().BeTrue();
        session.RecurrenceRule.Should().Be("FREQ=WEEKLY");
    }

    // --- State machine: Pending -> Confirmed -> InProgress -> Completed ---

    [Fact]
    public void Confirm_WhenPending_ShouldSucceed()
    {
        var session = CreateDefaultSession();
        session.ClearDomainEvents();

        var result = session.Confirm(ClockBeforeSchedule);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Confirmed);
        session.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<SessionConfirmedDomainEvent>();
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldFail()
    {
        var session = CreateConfirmedSession();

        var result = session.Confirm(ClockBeforeSchedule);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.AlreadyConfirmed");
    }

    [Fact]
    public void Confirm_WhenCompleted_ShouldFail()
    {
        var session = CreateInProgressSession();
        session.Complete();

        var result = session.Confirm(ClockBeforeSchedule);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.InvalidStatusTransition");
    }

    [Fact]
    public void Confirm_WhenScheduledInPast_ShouldFail()
    {
        var session = CreateDefaultSession(); // scheduled 2025-06-15 10:00
        session.ClearDomainEvents();

        // Clock is after the scheduled time → confirming should be rejected.
        var result = session.Confirm(new DateTime(2025, 6, 16, 0, 0, 0, DateTimeKind.Utc));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.SessionInPast");
        session.Status.Should().Be(SessionStatus.Pending);
    }

    [Fact]
    public void StartSession_WhenConfirmed_ShouldSucceed()
    {
        var session = CreateConfirmedSession();

        var result = session.StartSession(DateTime.UtcNow, 15, true);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.InProgress);
    }

    // The default session is scheduled 2025-06-15 10:00 gym-local; the clock passed in
    // is gym-local wall-clock too (handlers convert from UTC).
    [Fact]
    public void StartSession_InsideEarlyWindow_ShouldSucceed()
    {
        var session = CreateConfirmedSession();

        var result = session.StartSession(new DateTime(2025, 6, 15, 9, 50, 0), 15, allowEarlyStart: false);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.InProgress);
    }

    [Fact]
    public void StartSession_BeforeEarlyWindow_ShouldFailUnlessEarlyStartAllowed()
    {
        var session = CreateConfirmedSession();
        var tooEarly = new DateTime(2025, 6, 15, 9, 30, 0);

        var blocked = session.StartSession(tooEarly, 15, allowEarlyStart: false);

        blocked.IsFailure.Should().BeTrue();
        blocked.Error.Code.Should().Be("Scheduling.SessionTooEarly");
        session.Status.Should().Be(SessionStatus.Confirmed);

        session.StartSession(tooEarly, 15, allowEarlyStart: true).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void StartSession_WhenPending_ShouldFail()
    {
        var session = CreateDefaultSession();

        var result = session.StartSession(DateTime.UtcNow, 15, true);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.InvalidStatusTransition");
    }

    [Fact]
    public void Complete_WhenInProgress_ShouldSucceed()
    {
        var session = CreateInProgressSession();
        session.ClearDomainEvents();

        var result = session.Complete();

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Completed);
        session.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<SessionCompletedDomainEvent>();
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldFail()
    {
        var session = CreateInProgressSession();
        session.Complete();

        var result = session.Complete();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.AlreadyCompleted");
    }

    [Fact]
    public void Complete_WhenPending_ShouldFail()
    {
        var session = CreateDefaultSession();

        var result = session.Complete();

        result.IsFailure.Should().BeTrue();
    }

    // --- Cancellation ---

    [Fact]
    public void Cancel_WhenPending_ByMember_ShouldSucceed()
    {
        var session = CreateDefaultSession();
        session.ClearDomainEvents();

        var result = session.Cancel("Changed plans", MemberId, isLateCancellation: false);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.CancelledByMember);
        session.CancellationReason.Should().Be("Changed plans");
        session.CancelledAt.Should().NotBeNull();
        session.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<SessionCancelledDomainEvent>();
    }

    [Fact]
    public void Cancel_WhenPending_ByTrainer_ShouldSucceed()
    {
        var session = CreateDefaultSession();

        var result = session.Cancel("Sick", TrainerId, isLateCancellation: false);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.CancelledByTrainer);
    }

    [Fact]
    public void Cancel_LateCancellation_ShouldSetLateCancelledStatus()
    {
        var session = CreateConfirmedSession();

        var result = session.Cancel("Last minute", MemberId, isLateCancellation: true);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.LateCancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldFail()
    {
        var session = CreateDefaultSession();
        session.Cancel("reason", MemberId, false);

        var result = session.Cancel("again", MemberId, false);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.AlreadyCancelled");
    }

    [Fact]
    public void Cancel_WhenInProgress_ShouldFail()
    {
        var session = CreateInProgressSession();

        var result = session.Cancel("reason", MemberId, false);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.InvalidStatusTransition");
    }

    [Fact]
    public void Cancel_WhenCompleted_ShouldFail()
    {
        var session = CreateInProgressSession();
        session.Complete();

        var result = session.Cancel("reason", MemberId, false);

        result.IsFailure.Should().BeTrue();
    }

    // --- NoShow ---

    [Fact]
    public void MarkNoShow_WhenPending_ShouldSucceed()
    {
        var session = CreateDefaultSession();
        session.ClearDomainEvents();

        var result = session.MarkNoShow();

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.NoShow);
        session.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<SessionNoShowDomainEvent>();
    }

    [Fact]
    public void MarkNoShow_WhenConfirmed_ShouldSucceed()
    {
        var session = CreateConfirmedSession();

        var result = session.MarkNoShow();

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.NoShow);
    }

    [Fact]
    public void MarkNoShow_WhenInProgress_ShouldFail()
    {
        var session = CreateInProgressSession();

        var result = session.MarkNoShow();

        result.IsFailure.Should().BeTrue();
    }

    // --- Trainer Log ---

    [Fact]
    public void LogTrainerSession_ValidInput_ShouldSucceed()
    {
        var session = CreateDefaultSession();
        session.ClearDomainEvents();

        var result = session.LogTrainerSession(new TrainerLogDetails
        {
            ExercisesPerformed = "Squats, Bench",
            ActualVsPlanned = "On track",
            ClientEffortRating = 4,
            Feedback = "Good session",
            TechniqueNotes = "Form improving",
            AreasToImprove = "Hamstrings",
            NextSessionFocus = "Lower body",
            AttendanceConfirmed = true
        });

        result.IsSuccess.Should().BeTrue();
        session.TrainerLog.Should().NotBeNull();
        session.TrainerLog!.ClientEffortRating.Should().Be(4);
        session.TrainerLog.AttendanceConfirmed.Should().BeTrue();
        session.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<TrainerLoggedSessionDomainEvent>();
    }

    [Fact]
    public void LogTrainerSession_DuplicateLog_ShouldFail()
    {
        var session = CreateDefaultSession();
        session.LogTrainerSession(ValidTrainerLog);

        var result = session.LogTrainerSession(ValidTrainerLog with { ExercisesPerformed = "More", ClientEffortRating = 4 });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.TrainerLogAlreadyExists");
    }

    public static TheoryData<TrainerLogDetails> TrainerLogsWithInvalidRating => new()
    {
        ValidTrainerLog with { ClientEffortRating = 0 },
        ValidTrainerLog with { ClientEffortRating = 6 },
    };

    [Theory]
    [MemberData(nameof(TrainerLogsWithInvalidRating))]
    public void LogTrainerSession_InvalidRating_ShouldFail(TrainerLogDetails log)
    {
        var session = CreateDefaultSession();

        var result = session.LogTrainerSession(log);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.InvalidRating");
    }

    // --- Member Log ---

    [Fact]
    public void LogMemberSession_ValidInput_ShouldSucceed()
    {
        var session = CreateCompletedSession();

        var result = session.LogMemberSession(ValidMemberLog with { PersonalNotes = "Felt great" });

        result.IsSuccess.Should().BeTrue();
        session.MemberLog.Should().NotBeNull();
        session.MemberLog!.Rating.Should().Be(5);
        session.MemberLog.PerceivedExertion.Should().Be(7);
        session.MemberLog.Satisfaction.Should().Be(5);
        session.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<MemberLoggedSessionDomainEvent>();
    }

    [Fact]
    public void LogMemberSession_DuplicateLog_ShouldFail()
    {
        var session = CreateCompletedSession();
        session.LogMemberSession(ValidMemberLog with { Rating = 4, PerceivedExertion = 6, Satisfaction = 4 });

        var result = session.LogMemberSession(ValidMemberLog);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.MemberLogAlreadyExists");
    }

    [Fact]
    public void LogMemberSession_NotCompleted_ShouldFail()
    {
        var session = CreateDefaultSession();

        var result = session.LogMemberSession(ValidMemberLog);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.MemberLogRequiresCompletedSession");
        session.MemberLog.Should().BeNull();
    }

    // Rating and satisfaction share the 1-5 scale (and its error); exertion is RPE 1-10.
    public static TheoryData<MemberLogDetails, Error> MemberLogsOutOfRange => new()
    {
        { ValidMemberLog with { Rating = 0 }, SchedulingErrors.InvalidRating },
        { ValidMemberLog with { Rating = 6 }, SchedulingErrors.InvalidRating },
        { ValidMemberLog with { PerceivedExertion = 0 }, SchedulingErrors.InvalidExertion },
        { ValidMemberLog with { PerceivedExertion = 11 }, SchedulingErrors.InvalidExertion },
        { ValidMemberLog with { Satisfaction = 0 }, SchedulingErrors.InvalidRating },
        { ValidMemberLog with { Satisfaction = 6 }, SchedulingErrors.InvalidRating },
    };

    [Theory]
    [MemberData(nameof(MemberLogsOutOfRange))]
    public void LogMemberSession_OutOfRangeScore_ShouldFail(MemberLogDetails log, Error expected)
    {
        var session = CreateCompletedSession();

        var result = session.LogMemberSession(log);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expected.Code);
    }

    // --- Full lifecycle ---

    [Fact]
    public void FullLifecycle_Pending_Confirmed_InProgress_Completed()
    {
        var session = CreateDefaultSession();

        session.Confirm(ClockBeforeSchedule).IsSuccess.Should().BeTrue();
        session.StartSession(DateTime.UtcNow, 15, true).IsSuccess.Should().BeTrue();
        session.Complete().IsSuccess.Should().BeTrue();

        session.Status.Should().Be(SessionStatus.Completed);
    }
}
