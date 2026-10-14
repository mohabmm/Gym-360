using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Events;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Domain.Entities;

public sealed class Session : Entity
{
    private static readonly SessionStatus[] CancellableStatuses =
    [
        SessionStatus.Pending,
        SessionStatus.Confirmed
    ];

    private readonly List<SessionExerciseLog> _exerciseLogs = new();

    private Session() : base() { }

    private Session(
        SessionParticipants participants,
        SessionSchedule schedule,
        SessionType sessionType) : base(Guid.NewGuid())
    {
        GymId = participants.GymId;
        TrainerId = participants.TrainerId;
        MemberId = participants.MemberId;
        ScheduledDate = schedule.Date;
        ScheduledTime = schedule.Time;
        DurationMinutes = schedule.DurationMinutes;
        SessionType = sessionType;
        Status = SessionStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid GymId { get; private set; }
    public Guid TrainerId { get; private set; }
    public Guid MemberId { get; private set; }
    public DateOnly ScheduledDate { get; private set; }
    public TimeOnly ScheduledTime { get; private set; }
    public int DurationMinutes { get; private set; }
    public SessionType SessionType { get; private set; }
    public string? Focus { get; private set; }
    public SessionStatus Status { get; private set; }
    public bool IsRecurring { get; private set; }
    public string? RecurrenceRule { get; private set; }
    public TrainerLog? TrainerLog { get; private set; }
    public MemberLog? MemberLog { get; private set; }
    public IReadOnlyCollection<SessionExerciseLog> ExerciseLogs => _exerciseLogs.AsReadOnly();
    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Session Create(
        SessionParticipants participants,
        SessionSchedule schedule,
        SessionType sessionType,
        string? focus = null)
    {
        var session = new Session(participants, schedule, sessionType) { Focus = focus };
        session.RaiseBooked();
        return session;
    }

    public static Session CreateRecurring(
        SessionParticipants participants,
        SessionSchedule schedule,
        SessionType sessionType,
        string? recurrenceRule)
    {
        var session = new Session(participants, schedule, sessionType)
        {
            IsRecurring = true,
            RecurrenceRule = recurrenceRule
        };
        session.RaiseBooked();
        return session;
    }

    private void RaiseBooked() =>
        RaiseDomainEvent(new SessionBookedDomainEvent(Id, TrainerId, MemberId, GymId));

    /// <param name="nowGymLocal">
    /// Current time as gym-local wall-clock — the same frame ScheduledDate/ScheduledTime
    /// are stored in. Callers convert from UTC; passing raw UTC skews the guard by the
    /// gym's UTC offset.
    /// </param>
    public Result Confirm(DateTime nowGymLocal)
    {
        if (Status == SessionStatus.Confirmed)
            return Result.Failure(SchedulingErrors.AlreadyConfirmed);

        if (Status != SessionStatus.Pending)
            return Result.Failure(SchedulingErrors.InvalidStatusTransition);

        // Reject confirming a session whose scheduled time has already passed
        // (a past booking request can still be cancelled or marked no-show).
        var scheduledStart = ScheduledDate.ToDateTime(ScheduledTime);
        if (scheduledStart <= nowGymLocal)
            return Result.Failure(SchedulingErrors.SessionInPast(scheduledStart));

        Status = SessionStatus.Confirmed;
        RaiseDomainEvent(new SessionConfirmedDomainEvent(Id));
        return Result.Success();
    }

    /// <param name="nowGymLocal">Current time as gym-local wall-clock (see <see cref="Confirm"/>).</param>
    public Result StartSession(DateTime nowGymLocal, int earlyWindowMinutes, bool allowEarlyStart)
    {
        if (Status != SessionStatus.Confirmed)
            return Result.Failure(SchedulingErrors.InvalidStatusTransition);

        if (!allowEarlyStart)
        {
            var scheduledStart = ScheduledDate.ToDateTime(ScheduledTime);
            if (nowGymLocal < scheduledStart.AddMinutes(-earlyWindowMinutes))
                return Result.Failure(SchedulingErrors.SessionTooEarly(scheduledStart, earlyWindowMinutes));
        }

        Status = SessionStatus.InProgress;
        return Result.Success();
    }

    public Result Complete()
    {
        if (Status == SessionStatus.Completed)
            return Result.Failure(SchedulingErrors.AlreadyCompleted);

        if (Status != SessionStatus.InProgress)
            return Result.Failure(SchedulingErrors.InvalidStatusTransition);

        Status = SessionStatus.Completed;
        RaiseDomainEvent(new SessionCompletedDomainEvent(Id, TrainerId, MemberId, GymId));
        return Result.Success();
    }

    public Result Cancel(string reason, Guid cancelledBy, bool isLateCancellation)
    {
        if (Status is SessionStatus.CancelledByTrainer or SessionStatus.CancelledByMember or SessionStatus.LateCancelled)
            return Result.Failure(SchedulingErrors.AlreadyCancelled);

        if (!CancellableStatuses.Contains(Status))
            return Result.Failure(SchedulingErrors.InvalidStatusTransition);

        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;

        if (isLateCancellation)
        {
            Status = SessionStatus.LateCancelled;
        }
        else
        {
            Status = cancelledBy == TrainerId
                ? SessionStatus.CancelledByTrainer
                : SessionStatus.CancelledByMember;
        }

        RaiseDomainEvent(new SessionCancelledDomainEvent(Id, cancelledBy, isLateCancellation));
        return Result.Success();
    }

    public Result MarkNoShow()
    {
        if (!CancellableStatuses.Contains(Status))
            return Result.Failure(SchedulingErrors.InvalidStatusTransition);

        Status = SessionStatus.NoShow;
        RaiseDomainEvent(new SessionNoShowDomainEvent(Id, MemberId, GymId));
        return Result.Success();
    }

    public Result LogTrainerSession(TrainerLogDetails details)
    {
        if (TrainerLog is not null)
            return Result.Failure(SchedulingErrors.TrainerLogAlreadyExists);

        if (!IsInRange(details.ClientEffortRating, 1, 5))
            return Result.Failure(SchedulingErrors.InvalidRating);

        TrainerLog = TrainerLog.Create(Id, details);

        RaiseDomainEvent(new TrainerLoggedSessionDomainEvent(Id, TrainerId));
        return Result.Success();
    }

    public Result LogMemberSession(MemberLogDetails details)
    {
        if (Status != SessionStatus.Completed)
            return Result.Failure(SchedulingErrors.MemberLogRequiresCompletedSession);

        if (MemberLog is not null)
            return Result.Failure(SchedulingErrors.MemberLogAlreadyExists);

        var validation = ValidateMemberLog(details);
        if (validation.IsFailure)
            return validation;

        MemberLog = MemberLog.Create(Id, details);

        RaiseDomainEvent(new MemberLoggedSessionDomainEvent(Id, MemberId));
        return Result.Success();
    }

    public Result<SessionExerciseLog> AddExerciseLog(ExerciseLogDetails details)
    {
        if (Status != SessionStatus.Completed)
            return Result.Failure<SessionExerciseLog>(SchedulingErrors.ExerciseLogRequiresCompletedSession);

        var log = SessionExerciseLog.Create(Id, details);

        _exerciseLogs.Add(log);
        RaiseDomainEvent(new ExerciseLoggedDomainEvent(Id, log.Id, TrainerId, MemberId));
        return Result.Success(log);
    }

    public Result UpdateExerciseLog(Guid logId, ExerciseLogDetails details)
    {
        if (Status != SessionStatus.Completed)
            return Result.Failure(SchedulingErrors.ExerciseLogRequiresCompletedSession);

        var log = _exerciseLogs.FirstOrDefault(l => l.Id == logId);
        if (log is null)
            return Result.Failure(SchedulingErrors.ExerciseLogNotFound(logId));

        log.Apply(details);
        return Result.Success();
    }

    public Result RemoveExerciseLog(Guid logId)
    {
        var log = _exerciseLogs.FirstOrDefault(l => l.Id == logId);
        if (log is null)
            return Result.Failure(SchedulingErrors.ExerciseLogNotFound(logId));

        _exerciseLogs.Remove(log);
        return Result.Success();
    }

    private static Result ValidateMemberLog(MemberLogDetails details)
    {
        if (!IsInRange(details.Rating, 1, 5))
            return Result.Failure(SchedulingErrors.InvalidRating);

        if (!IsInRange(details.PerceivedExertion, 1, 10))
            return Result.Failure(SchedulingErrors.InvalidExertion);

        if (!IsInRange(details.Satisfaction, 1, 5))
            return Result.Failure(SchedulingErrors.InvalidRating);

        return Result.Success();
    }

    private static bool IsInRange(int value, int min, int max) => value >= min && value <= max;
}
