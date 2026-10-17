using GymApp.Common.Domain;
using GymApp.Modules.Progress.Domain.DomainEvents;
using GymApp.Modules.Progress.Domain.Enums;

namespace GymApp.Modules.Progress.Domain.Entities;

public sealed class Streak : Entity
{
    private static readonly int[] Milestones = { 7, 30, 60, 90, 180, 365 };

    private Streak() { }

    private Streak(
        Guid id,
        Guid userId,
        Guid gymId,
        StreakType streakType)
        : base(id)
    {
        UserId = userId;
        GymId = gymId;
        StreakType = streakType;
        CurrentStreak = 0;
        LongestStreak = 0;
        LastActivityDate = DateOnly.MinValue;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public Guid GymId { get; private set; }
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public StreakType StreakType { get; private set; }
    public DateOnly LastActivityDate { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Result<Streak> Create(
        Guid userId,
        Guid gymId,
        StreakType streakType)
    {
        var streak = new Streak(
            Guid.NewGuid(),
            userId,
            gymId,
            streakType);

        return Result.Success(streak);
    }

    public void RecordActivity(DateOnly date)
    {
        if (date <= LastActivityDate)
        {
            return;
        }

        bool isFirstActivity = LastActivityDate == DateOnly.MinValue;

        // For weekly streaks, only increment when moving to a new week
        if (!isFirstActivity && IsRepeatWithinSameWeek(date))
        {
            TouchLastActivity(date);
            return;
        }

        CurrentStreak = isFirstActivity || IsContinuation(date) ? CurrentStreak + 1 : 1;
        TouchLastActivity(date);

        if (CurrentStreak > LongestStreak)
        {
            LongestStreak = CurrentStreak;
        }

        RaiseMilestoneIfReached();
    }

    public void ResetIfBroken(DateOnly today)
    {
        if (LastActivityDate == DateOnly.MinValue)
        {
            return;
        }

        bool isBroken = StreakType switch
        {
            StreakType.DailyWorkout or StreakType.DailyCheckIn =>
                today.DayNumber - LastActivityDate.DayNumber > 1,
            StreakType.WeeklyWorkout =>
                GetIsoWeekNumber(today) - GetIsoWeekNumber(LastActivityDate) > 1,
            _ => false
        };

        if (isBroken)
        {
            CurrentStreak = 0;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    private bool IsContinuation(DateOnly date) => StreakType switch
    {
        StreakType.DailyWorkout or StreakType.DailyCheckIn =>
            date.DayNumber - LastActivityDate.DayNumber == 1,
        StreakType.WeeklyWorkout =>
            GetIsoWeekNumber(date) - GetIsoWeekNumber(LastActivityDate) == 1
            || IsSameIsoWeek(date, LastActivityDate),
        _ => false
    };

    private bool IsRepeatWithinSameWeek(DateOnly date) =>
        StreakType == StreakType.WeeklyWorkout && IsSameIsoWeek(date, LastActivityDate);

    private void TouchLastActivity(DateOnly date)
    {
        LastActivityDate = date;
        UpdatedAt = DateTime.UtcNow;
    }

    private void RaiseMilestoneIfReached()
    {
        if (!Milestones.Contains(CurrentStreak))
        {
            return;
        }

        RaiseDomainEvent(new StreakMilestoneReachedDomainEvent(
            UserId,
            GymId,
            StreakType.ToString(),
            CurrentStreak));
    }

    private static bool IsSameIsoWeek(DateOnly a, DateOnly b) =>
        GetIsoWeekNumber(a) == GetIsoWeekNumber(b);

    private static int GetIsoWeekNumber(DateOnly date)
    {
        return System.Globalization.ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue));
    }
}
