namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>
/// mem.first_month_challenge: the gym-local days a new member has to reach the target, both
/// ends included. It starts on the join date, or on the first plan's start when the gym has
/// no join date for the member.
/// </summary>
public sealed record ChallengeWindow
{
    public required DateOnly Start { get; init; }
    /// <summary>The last day that counts.</summary>
    public required DateOnly End { get; init; }

    public static ChallengeWindow Of(DateOnly start, int days) => new() { Start = start, End = start.AddDays(Math.Max(days, 1) - 1) };

    /// <summary>The join date wins; null when there is neither (no window yet).</summary>
    public static ChallengeWindow? For(DateOnly? joinedOn, DateOnly? firstPlanStart, int days) =>
        (joinedOn ?? firstPlanStart) is { } start ? Of(start, days) : null;

    public bool Contains(DateOnly day) => day >= Start && day <= End;

    public bool HasEnded(DateOnly today) => today > End;

    /// <summary>Days left including today; 0 once the window is over.</summary>
    public int DaysLeft(DateOnly today) =>
        HasEnded(today) ? 0 : End.DayNumber - (today < Start ? Start : today).DayNumber + 1;

    /// <summary>Workouts that count: not voided, done inside the window.</summary>
    public int Count(IEnumerable<ChallengeWorkout> workouts) =>
        workouts.Count(w => !w.IsVoided && Contains(w.CompletedOn));
}

/// <summary>What the challenge needs to know about one plan workout.</summary>
public readonly record struct ChallengeWorkout(DateOnly CompletedOn, bool IsVoided);
