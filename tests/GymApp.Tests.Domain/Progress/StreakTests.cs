using GymApp.Modules.Progress.Domain.DomainEvents;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;

namespace GymApp.Tests.Domain.Progress;

public sealed class StreakTests
{
    private static Streak CreateDefaultStreak(StreakType type = StreakType.DailyWorkout)
    {
        var result = Streak.Create(Guid.NewGuid(), Guid.NewGuid(), type);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    // A fresh streak with activity recorded, in order, on the given days of January 2025.
    private static Streak StreakActiveOnJanuary(StreakType type, params int[] days)
    {
        var streak = CreateDefaultStreak(type);
        foreach (var day in days)
            streak.RecordActivity(new DateOnly(2025, 1, day));
        return streak;
    }

    [Fact]
    public void Create_ShouldInitializeWithZeroStreak()
    {
        var streak = CreateDefaultStreak();

        streak.CurrentStreak.Should().Be(0);
        streak.LongestStreak.Should().Be(0);
        streak.LastActivityDate.Should().Be(DateOnly.MinValue);
    }

    [Fact]
    public void Create_ShouldReturnSuccessResult()
    {
        var result = Streak.Create(Guid.NewGuid(), Guid.NewGuid(), StreakType.DailyWorkout);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    // --- Daily streak ---

    [Fact]
    public void RecordActivity_FirstDay_ShouldStartStreakAtOne()
    {
        var streak = CreateDefaultStreak();
        var today = new DateOnly(2025, 1, 1);

        streak.RecordActivity(today);

        streak.CurrentStreak.Should().Be(1);
        streak.LongestStreak.Should().Be(1);
        streak.LastActivityDate.Should().Be(today);
    }

    [Fact]
    public void RecordActivity_ConsecutiveDays_ShouldIncrementStreak()
    {
        var streak = StreakActiveOnJanuary(StreakType.DailyWorkout, 1, 2, 3);

        streak.CurrentStreak.Should().Be(3);
        streak.LongestStreak.Should().Be(3);
    }

    [Fact]
    public void RecordActivity_MissedDay_ShouldResetToOne()
    {
        var streak = StreakActiveOnJanuary(StreakType.DailyWorkout, 1, 2, 4); // skipped Jan 3

        streak.CurrentStreak.Should().Be(1);
        streak.LongestStreak.Should().Be(2);
    }

    [Fact]
    public void RecordActivity_SameDay_ShouldIgnore()
    {
        var streak = CreateDefaultStreak();
        var today = new DateOnly(2025, 1, 1);

        streak.RecordActivity(today);
        streak.RecordActivity(today);

        streak.CurrentStreak.Should().Be(1);
    }

    [Fact]
    public void RecordActivity_PastDate_ShouldIgnore()
    {
        var streak = CreateDefaultStreak();

        streak.RecordActivity(new DateOnly(2025, 1, 2));
        streak.RecordActivity(new DateOnly(2025, 1, 1)); // past

        streak.CurrentStreak.Should().Be(1);
        streak.LastActivityDate.Should().Be(new DateOnly(2025, 1, 2));
    }

    [Fact]
    public void RecordActivity_LongestStreak_ShouldNotDecreaseAfterReset()
    {
        var streak = CreateDefaultStreak();

        // Build 5-day streak
        for (int i = 1; i <= 5; i++)
            streak.RecordActivity(new DateOnly(2025, 1, i));

        // Break and start new
        streak.RecordActivity(new DateOnly(2025, 1, 10));

        streak.CurrentStreak.Should().Be(1);
        streak.LongestStreak.Should().Be(5);
    }

    // --- Milestone events ---

    [Theory]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(365)]
    public void RecordActivity_AtMilestone_ShouldRaiseMilestoneEvent(int milestone)
    {
        var streak = CreateDefaultStreak();
        var startDate = new DateOnly(2025, 1, 1);

        for (int i = 0; i < milestone; i++)
        {
            streak.ClearDomainEvents();
            streak.RecordActivity(startDate.AddDays(i));
        }

        streak.CurrentStreak.Should().Be(milestone);
        streak.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<StreakMilestoneReachedDomainEvent>()
            .Which.MilestoneDay.Should().Be(milestone);
    }

    [Fact]
    public void RecordActivity_NotAtMilestone_ShouldNotRaiseEvent()
    {
        var streak = CreateDefaultStreak();
        var startDate = new DateOnly(2025, 1, 1);

        // Build 5-day streak (not a milestone)
        for (int i = 0; i < 5; i++)
        {
            streak.ClearDomainEvents();
            streak.RecordActivity(startDate.AddDays(i));
        }

        streak.GetDomainEvents().Should().BeEmpty();
    }

    // --- Weekly streak ---

    [Fact]
    public void RecordActivity_WeeklyStreak_SameWeek_ShouldNotIncrement()
    {
        var streak = CreateDefaultStreak(StreakType.WeeklyWorkout);

        // Monday and Tuesday of the same week
        streak.RecordActivity(new DateOnly(2025, 1, 6)); // Monday
        streak.RecordActivity(new DateOnly(2025, 1, 7)); // Tuesday

        streak.CurrentStreak.Should().Be(1);
    }

    [Fact]
    public void RecordActivity_WeeklyStreak_ConsecutiveWeeks_ShouldIncrement()
    {
        var streak = CreateDefaultStreak(StreakType.WeeklyWorkout);

        streak.RecordActivity(new DateOnly(2025, 1, 6));  // Week 2
        streak.RecordActivity(new DateOnly(2025, 1, 13)); // Week 3

        streak.CurrentStreak.Should().Be(2);
    }

    [Fact]
    public void RecordActivity_WeeklyStreak_SkippedWeek_ShouldReset()
    {
        var streak = CreateDefaultStreak(StreakType.WeeklyWorkout);

        streak.RecordActivity(new DateOnly(2025, 1, 6));  // Week 2
        streak.RecordActivity(new DateOnly(2025, 1, 20)); // Week 4, skipped Week 3

        streak.CurrentStreak.Should().Be(1);
    }

    // --- ResetIfBroken ---

    [Fact]
    public void ResetIfBroken_WhenDailyStreakBroken_ShouldResetCurrent()
    {
        var streak = StreakActiveOnJanuary(StreakType.DailyWorkout, 1, 2);

        streak.ResetIfBroken(new DateOnly(2025, 1, 4)); // missed Jan 3

        streak.CurrentStreak.Should().Be(0);
        streak.LongestStreak.Should().Be(2);
    }

    [Fact]
    public void ResetIfBroken_WhenNotBroken_ShouldDoNothing()
    {
        var streak = CreateDefaultStreak();
        streak.RecordActivity(new DateOnly(2025, 1, 1));

        streak.ResetIfBroken(new DateOnly(2025, 1, 2)); // consecutive

        streak.CurrentStreak.Should().Be(1);
    }

    [Fact]
    public void ResetIfBroken_WhenNoActivity_ShouldDoNothing()
    {
        var streak = CreateDefaultStreak();

        streak.ResetIfBroken(new DateOnly(2025, 1, 10));

        streak.CurrentStreak.Should().Be(0);
    }

    [Fact]
    public void ResetIfBroken_WeeklyStreak_SkippedWeek_ShouldReset()
    {
        var streak = CreateDefaultStreak(StreakType.WeeklyWorkout);
        streak.RecordActivity(new DateOnly(2025, 1, 6)); // Week 2

        streak.ResetIfBroken(new DateOnly(2025, 1, 20)); // Week 4

        streak.CurrentStreak.Should().Be(0);
    }

    // --- DailyCheckIn streak type ---

    [Fact]
    public void RecordActivity_DailyCheckIn_ConsecutiveDays_ShouldIncrement() =>
        StreakActiveOnJanuary(StreakType.DailyCheckIn, 1, 2, 3).CurrentStreak.Should().Be(3);
}
