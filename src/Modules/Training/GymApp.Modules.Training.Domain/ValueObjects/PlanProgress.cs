using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>Workouts this week (Saturday to Friday) and this month, against the plan's days per week.</summary>
public sealed record PlanWeekStats(int ThisWeek, int WeekTarget, int ThisMonth, DateOnly WeekStart, DateOnly? LastWorkoutOn);

public sealed record WeightPoint(DateOnly On, decimal WeightKg);

/// <summary>Heaviest weight per workout for one exercise, oldest first.</summary>
public sealed record ExerciseTrend(string Identity, string Name, string? NameAr, IReadOnlyList<WeightPoint> Points);

/// <summary>Counts and weight history from finished plan workouts. Voided workouts never count.</summary>
public static class PlanProgress
{
    public const int MaxTrendPoints = 12;
    public const int MaxTrends = 20;

    public static PlanWeekStats Stats(IEnumerable<PlanWorkoutCompletion> workouts, DateOnly today, int weekTarget)
    {
        var counted = workouts.Where(w => !w.IsVoided).ToList();
        var weekStart = PlanCalendar.WeekStart(today);
        var monthStart = PlanCalendar.MonthStart(today);
        return new PlanWeekStats(
            counted.Count(w => w.CompletedOn >= weekStart && w.CompletedOn <= today),
            weekTarget,
            counted.Count(w => w.CompletedOn >= monthStart && w.CompletedOn <= today),
            weekStart,
            counted.Count == 0 ? null : counted.Max(w => w.CompletedOn));
    }

    /// <summary>The last weight the trainee used for each exercise of the plan, by exercise key — to prefill the next workout.</summary>
    public static IReadOnlyDictionary<string, decimal> LastWeights(PlanDocument document, IEnumerable<PlanWorkoutCompletion> workouts)
    {
        var latest = new Dictionary<string, decimal>();
        foreach (var set in workouts.Where(w => !w.IsVoided).OrderBy(w => w.CompletedAt).SelectMany(w => w.Sets))
        {
            if (set is { Done: true, WeightKg: { } kg })
                latest[set.Identity()] = kg;
        }
        return document.Sequence()
            .SelectMany(d => d.Exercises)
            .Select(e => (e.Key, Found: latest.TryGetValue(ExerciseIdentity.Of(e), out var kg), Kg: kg))
            .Where(x => x.Found)
            .ToDictionary(x => x.Key, x => x.Kg);
    }

    public static IReadOnlyList<ExerciseTrend> Trends(IEnumerable<PlanWorkoutCompletion> workouts)
    {
        var heaviest = workouts
            .Where(w => !w.IsVoided)
            .OrderBy(w => w.CompletedAt)
            .SelectMany(w => w.Sets.Where(s => s is { Done: true, WeightKg: > 0 }).Select(s => (w.CompletedOn, Set: s)))
            .GroupBy(x => x.Set.Identity());

        return heaviest
            .Select(ToTrend)
            .OrderByDescending(t => t.Points.Count)
            .Take(MaxTrends)
            .ToList();
    }

    private static ExerciseTrend ToTrend(IGrouping<string, (DateOnly On, CompletedSet Set)> sets)
    {
        var points = sets
            .GroupBy(x => x.On)
            .Select(day => new WeightPoint(day.Key, day.Max(x => x.Set.WeightKg!.Value)))
            .TakeLast(MaxTrendPoints)
            .ToList();
        var last = sets.Last().Set;
        return new ExerciseTrend(sets.Key, last.ExerciseName, last.ExerciseNameAr, points);
    }
}
