namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>A workout day at its place in the plan: index 0 = week 1 day 1.</summary>
public sealed record PlanSlot(int Index, int Week, PlanDay Day);

/// <summary>A finished workout as the cursor sees it: which day, at which place it was logged, on which gym-local date.</summary>
public sealed record CompletionMark(string DayKey, int SequenceIndex, DateOnly CompletedOn);

/// <summary>
/// "What's my workout today?" Plan days are a sequence, not dates: a missed day just waits.
/// The next workout is the day after the furthest one done; when the day was removed by a
/// coach edit the place it was logged at is used instead.
/// </summary>
public sealed record PlanCursor(
    PlanSlot? Next,
    int LastDoneIndex,
    int Total,
    bool DoneToday,
    IReadOnlyList<string> DoneKeys)
{
    public bool Finished => Next is null && Total > 0;

    public static IReadOnlyList<PlanSlot> Slots(PlanDocument document)
    {
        return document.Weeks
            .OrderBy(w => w.Week)
            .SelectMany(week => week.Days.Select(day => (week.Week, day)))
            .Select((pair, index) => new PlanSlot(index, pair.Week, pair.day))
            .ToList();
    }

    public static PlanSlot? Find(PlanDocument document, string dayKey) =>
        Slots(document).FirstOrDefault(s => s.Day.Key == dayKey);

    public static PlanCursor For(PlanDocument document, IReadOnlyCollection<CompletionMark> marks, DateOnly today)
    {
        var slots = Slots(document);
        var keys = slots.ToDictionary(s => s.Day.Key, s => s.Index);
        var last = marks.Count == 0
            ? -1
            : marks.Max(m => keys.TryGetValue(m.DayKey, out var index) ? index : Math.Min(m.SequenceIndex, slots.Count - 1));
        var next = last + 1 < slots.Count ? slots[last + 1] : null;
        var doneKeys = marks.Select(m => m.DayKey).Where(keys.ContainsKey).Distinct().ToList();
        return new PlanCursor(next, last, slots.Count, marks.Any(m => m.CompletedOn == today), doneKeys);
    }
}
