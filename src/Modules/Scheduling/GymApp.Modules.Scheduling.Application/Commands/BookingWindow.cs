using GymApp.Modules.Scheduling.Application.Queries;
using GymApp.Modules.Scheduling.Domain.Entities;

namespace GymApp.Modules.Scheduling.Application.Commands;

/// <summary>
/// A requested session time on a trainer's day, padded by the trainer's buffer.
/// Shared by the single and recurring booking commands to detect double bookings.
/// </summary>
internal sealed record BookingWindow(TimeOnly Start, int DurationMinutes, int BufferMinutes)
{
    /// <summary>True when any slot-holding session on the same day overlaps this window.</summary>
    public bool ConflictsWithAny(IEnumerable<Session> existingSessions) =>
        existingSessions.Where(SlotGrid.HoldsSlot).Any(Overlaps);

    private bool Overlaps(Session existing)
    {
        var existingEnd = existing.ScheduledTime.AddMinutes(existing.DurationMinutes + BufferMinutes);
        var newEnd = Start.AddMinutes(DurationMinutes).AddMinutes(BufferMinutes);

        return Start < existingEnd && existing.ScheduledTime < newEnd;
    }
}
