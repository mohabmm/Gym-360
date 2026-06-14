using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Events;

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
        Guid id,
        Guid gymId,
        Guid trainerId,
        Guid memberId,
        DateOnly scheduledDate,
        TimeOnly scheduledTime,
        int durationMinutes,
        SessionType sessionType,
        string? focus,
        bool isRecurring,
        string? recurrenceRule) : base(id)
    {
        GymId = gymId;
        TrainerId = trainerId;
        MemberId = memberId;
        ScheduledDate = scheduledDate;
        ScheduledTime = scheduledTime;
        DurationMinutes = durationMinutes;
        SessionType = sessionType;
        Focus = focus;
        Status = SessionStatus.Pending;
        IsRecurring = isRecurring;
        RecurrenceRule = recurrenceRule;
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
        Guid gymId,
        Guid trainerId,
        Guid memberId,
        DateOnly scheduledDate,
        TimeOnly scheduledTime,
        int durationMinutes,
        SessionType sessionType,
        string? focus = null,
        bool isRecurring = false,
        string? recurrenceRule = null)
    {
        var session = new Session(
            Guid.NewGuid(),
            gymId,
            trainerId,
            memberId,
            scheduledDate,
            scheduledTime,
            durationMinutes,
            sessionType,
            focus,
            isRecurring,
            recurrenceRule);

        session.RaiseDomainEvent(new SessionBookedDomainEvent(
            session.Id, trainerId, memberId, gymId));

        return session;
    }

    public Result Confirm(DateTime nowUtc)
    {
        if (Status == SessionStatus.Confirmed)
            return Result.Failure(SchedulingErrors.AlreadyConfirmed);

        if (Status != SessionStatus.Pending)
            return Result.Failure(SchedulingErrors.InvalidStatusTransition);

        // Reject confirming a session whose scheduled time has already passed
        // (a past booking request can still be cancelled or marked no-show).
        // Mirrors StartSession's UTC treatment of the stored wall-clock time.
        var scheduledStartUtc = ScheduledDate.ToDateTime(ScheduledTime, DateTimeKind.Utc);
        if (scheduledStartUtc <= nowUtc)
            return Result.Failure(SchedulingErrors.SessionInPast(scheduledStartUtc));

        Status = SessionStatus.Confirmed;
        RaiseDomainEvent(new SessionConfirmedDomainEvent(Id));
        return Result.Success();
    }

    public Result StartSession(DateTime nowUtc, int earlyWindowMinutes, bool allowEarlyStart)
    {
        if (Status != SessionStatus.Confirmed)
            return Result.Failure(SchedulingErrors.InvalidStatusTransition);

        if (!allowEarlyStart)
        {
            var scheduledStartUtc = ScheduledDate.ToDateTime(ScheduledTime, DateTimeKind.Utc);
            if (nowUtc < scheduledStartUtc.AddMinutes(-earlyWindowMinutes))
                return Result.Failure(SchedulingErrors.SessionTooEarly(scheduledStartUtc, earlyWindowMinutes));
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

    public Result LogTrainerSession(
        string exercisesPerformed,
        string? actualVsPlanned,
        int clientEffortRating,
        string? feedback,
        string? techniqueNotes,
        string? areasToImprove,
        string? nextSessionFocus,
        bool attendanceConfirmed)
    {
        if (TrainerLog is not null)
            return Result.Failure(SchedulingErrors.TrainerLogAlreadyExists);

        if (clientEffortRating < 1 || clientEffortRating > 5)
            return Result.Failure(SchedulingErrors.InvalidRating);

        TrainerLog = TrainerLog.Create(
            Id,
            exercisesPerformed,
            actualVsPlanned,
            clientEffortRating,
            feedback,
            techniqueNotes,
            areasToImprove,
            nextSessionFocus,
            attendanceConfirmed);

        RaiseDomainEvent(new TrainerLoggedSessionDomainEvent(Id, TrainerId));
        return Result.Success();
    }

    public Result LogMemberSession(
        int rating,
        int perceivedExertion,
        string? personalNotes,
        string? discomfort,
        int satisfaction)
    {
        if (Status != SessionStatus.Completed)
            return Result.Failure(SchedulingErrors.MemberLogRequiresCompletedSession);

        if (MemberLog is not null)
            return Result.Failure(SchedulingErrors.MemberLogAlreadyExists);

        if (rating < 1 || rating > 5)
            return Result.Failure(SchedulingErrors.InvalidRating);

        if (perceivedExertion < 1 || perceivedExertion > 10)
            return Result.Failure(SchedulingErrors.InvalidExertion);

        if (satisfaction < 1 || satisfaction > 5)
            return Result.Failure(SchedulingErrors.InvalidRating);

        MemberLog = MemberLog.Create(
            Id,
            rating,
            perceivedExertion,
            personalNotes,
            discomfort,
            satisfaction);

        RaiseDomainEvent(new MemberLoggedSessionDomainEvent(Id, MemberId));
        return Result.Success();
    }

    public Result<SessionExerciseLog> AddExerciseLog(
        Guid? exerciseId,
        string exerciseName,
        string? exerciseNameAr,
        int orderIndex,
        int sets,
        string reps,
        decimal? weight,
        string? notes)
    {
        if (Status != SessionStatus.Completed)
            return Result.Failure<SessionExerciseLog>(SchedulingErrors.ExerciseLogRequiresCompletedSession);

        var log = SessionExerciseLog.Create(
            Id, exerciseId, exerciseName, exerciseNameAr, orderIndex, sets, reps, weight, notes);

        _exerciseLogs.Add(log);
        RaiseDomainEvent(new ExerciseLoggedDomainEvent(Id, log.Id, TrainerId, MemberId));
        return Result.Success(log);
    }

    public Result UpdateExerciseLog(
        Guid logId,
        Guid? exerciseId,
        string exerciseName,
        string? exerciseNameAr,
        int orderIndex,
        int sets,
        string reps,
        decimal? weight,
        string? notes)
    {
        if (Status != SessionStatus.Completed)
            return Result.Failure(SchedulingErrors.ExerciseLogRequiresCompletedSession);

        var log = _exerciseLogs.FirstOrDefault(l => l.Id == logId);
        if (log is null)
            return Result.Failure(SchedulingErrors.ExerciseLogNotFound(logId));

        log.Apply(exerciseId, exerciseName, exerciseNameAr, orderIndex, sets, reps, weight, notes);
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
}
