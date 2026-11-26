using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.QuickLogSession;

/// <summary>
/// pt.dual_session_logging: the coach records a session that happened, no booking needed.
/// <paramref name="Date"/> / <paramref name="StartTime"/> (gym-local) say when it happened — up to
/// <see cref="QuickLogSchedule.MaxDaysBack"/> days back; omitted, it is a session going on
/// now. A session in progress may be recorded; one that has not started may not. <paramref name="DurationMinutes"/> defaults to 60. <paramref name="PlanDay"/> (optional,
/// pt.training_plan) is the plan day done in the session; it changes nothing about the session.
/// </summary>
public sealed record QuickLogSessionCommand(
    Guid GymId,
    Guid TrainerId,
    Guid MemberId,
    string? Notes,
    DateOnly? Date = null,
    TimeOnly? StartTime = null,
    int? DurationMinutes = null,
    QuickLogPlanDay? PlanDay = null) : ICommand<QuickLogSessionResult>;

/// <summary>Which day of the trainee's training plan the session covered.</summary>
public sealed record QuickLogPlanDay(Guid PlanId, string DayKey);

/// <param name="RemainingSessions">Sessions left with this coach after this one is counted.</param>
/// <param name="AlreadyLogged">True when this session was already recorded (same slot, or a double tap): nothing new was logged.</param>
/// <param name="Date">Gym-local day of the recorded session.</param>
/// <param name="StartTime">Gym-local start time of the recorded session.</param>
public sealed record QuickLogSessionResult(
    Guid SessionId,
    int RemainingSessions,
    bool AlreadyLogged,
    DateOnly Date,
    TimeOnly StartTime);
