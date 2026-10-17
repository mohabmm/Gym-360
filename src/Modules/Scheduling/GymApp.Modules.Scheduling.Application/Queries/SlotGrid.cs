using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;

namespace GymApp.Modules.Scheduling.Application.Queries;

/// <summary>
/// Splits a trainer's availability windows into bookable slots of <see cref="Duration"/>,
/// separated by <see cref="Buffer"/>. Shared by the available-slots and weekly-availability queries.
/// </summary>
internal sealed record SlotGrid(TimeSpan Duration, TimeSpan Buffer)
{
    public static SlotGrid For(TrainerAvailability availability) => new(
        TimeSpan.FromMinutes(availability.SessionDurationMinutes),
        TimeSpan.FromMinutes(Math.Max(0, availability.BufferMinutes)));

    /// <summary>Cancelled and no-show sessions free their slot; every other status holds it.</summary>
    public static bool HoldsSlot(Session session) =>
        session.Status is not (SessionStatus.CancelledByTrainer
            or SessionStatus.CancelledByMember
            or SessionStatus.LateCancelled
            or SessionStatus.NoShow);

    // Arithmetic is done on TimeSpan, NOT TimeOnly: TimeOnly.AddMinutes wraps past
    // midnight, so a window ending late in the evening (e.g. 23:59) made
    // "start + duration <= end" true forever — the request never returned and the
    // slot list grew until the process ran out of memory.
    public IEnumerable<(TimeSpan Start, TimeSpan End)> SlotsIn(IEnumerable<AvailabilitySlot> windows)
    {
        // A non-positive duration would never advance the cursor.
        if (Duration <= TimeSpan.Zero)
            yield break;

        foreach (var window in windows)
        {
            var windowEnd = window.EndTime.ToTimeSpan();
            for (var start = window.StartTime.ToTimeSpan(); start + Duration <= windowEnd; start += Duration + Buffer)
                yield return (start, start + Duration);
        }
    }

    /// <summary>True when the session, padded by the buffer, overlaps the slot.</summary>
    public bool Overlaps(Session session, (TimeSpan Start, TimeSpan End) slot)
    {
        var sessionStart = session.ScheduledTime.ToTimeSpan();
        var sessionEnd = sessionStart + TimeSpan.FromMinutes(session.DurationMinutes) + Buffer;
        return slot.Start < sessionEnd && sessionStart < slot.End + Buffer;
    }
}
