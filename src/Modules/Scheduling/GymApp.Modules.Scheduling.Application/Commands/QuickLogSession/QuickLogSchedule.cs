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

    /// <summary>Clock drift / "I'm wrapping up" slack for the end-of-session check.</summary>
    private static readonly TimeSpan EndSlack = TimeSpan.FromMinutes(5);

    /// <summary>The requested slot; without a date and time, a session that just finished.</summary>
    public static SessionSchedule Resolve(QuickLogSessionCommand request, DateTime nowLocal)
    {
        var duration = request.DurationMinutes ?? DefaultDurationMinutes;
        if (request.StartTime is not { } start)
        {
            var justFinished = nowLocal.AddMinutes(-duration);
            return new SessionSchedule(DateOnly.FromDateTime(justFinished), TimeOnly.FromDateTime(justFinished), duration);
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

        var end = schedule.Date.ToDateTime(schedule.Time).AddMinutes(schedule.DurationMinutes);
        return end > nowLocal + EndSlack ? SchedulingErrors.QuickLogNotEnded : null;
    }

    public static DateTime NowLocal(DateTime nowUtc) => GymLocalTime.FromUtc(nowUtc);
}
