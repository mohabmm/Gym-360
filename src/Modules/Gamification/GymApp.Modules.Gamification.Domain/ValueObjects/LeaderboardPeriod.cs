namespace GymApp.Modules.Gamification.Domain.ValueObjects;

public sealed record LeaderboardPeriod
{
    public int Month { get; }
    public int Year { get; }

    private LeaderboardPeriod(int month, int year)
    {
        Month = month;
        Year = year;
    }

    public static LeaderboardPeriod Create(int month, int year)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");

        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 2000 and 2100.");

        return new LeaderboardPeriod(month, year);
    }
}
