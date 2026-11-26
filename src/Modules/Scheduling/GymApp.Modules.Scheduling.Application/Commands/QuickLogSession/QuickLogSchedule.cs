using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Application.Commands.QuickLogSession;

/// <summary>When a quick-logged session happened, and whether that is allowed.</summary>
internal static class QuickLogSchedule
{
    /// <summary>A coach may record a forgotten session up to this many days back.</summary>
    public const int MaxDaysBack = 7;

    public const int DefaultDurationMinutes = 60;
    public const int MinDurationMinutes = 15;
    public const int MaxDurationMinutes = 180;

    /// <summary>Clock drift slack: a session may be recorded as starting this much ahead of now.</summary>
    private static readonly TimeSpan StartSlack = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The requested slot; without a date and time, a session going on now (the coach records it
    /// while training the client) — started at the current quarter hour.
    /// </summary>
    public static SessionSchedule Resolve(QuickLogSessionCommand request, DateTime nowLocal)
    {
        var duration = request.DurationMinutes ?? DefaultDurationMinutes;
        if (request.StartTime is not { } start)
        {
            var now = TimeOnly.FromDateTime(nowLocal);
            return new SessionSchedule(
                DateOnly.FromDateTime(nowLocal), new TimeOnly(now.Hour, now.Minute - now.Minute % 15), duration);
        }

        return new SessionSchedule(request.Date ?? DateOnly.FromDateTime(nowLocal), start, duration);
    }

    /// <summary>The coach picked the day/time (vs. the old one-tap "just now").</summary>
    public static bool IsExplicit(QuickLogSessionCommand request) => request.StartTime is not null;

    /// <summary>Null when the slot is fine, otherwise why not.</summary>
    public static Error? Check(SessionSchedule schedule, DateTime nowLocal)
    {
        var today = DateOnly.FromDateTime(nowLocal);
        if (schedule.Date < today.AddDays(-MaxDaysBack))
            return SchedulingErrors.QuickLogTooOld;

        // A session in progress may be recorded (the coach logs it on the floor); one that has
        // not started yet may not.
        var start = schedule.Date.ToDateTime(schedule.Time);
        return start > nowLocal + StartSlack ? SchedulingErrors.QuickLogNotStarted : null;
    }

    public static DateTime NowLocal(DateTime nowUtc) => GymLocalTime.FromUtc(nowUtc);
}
