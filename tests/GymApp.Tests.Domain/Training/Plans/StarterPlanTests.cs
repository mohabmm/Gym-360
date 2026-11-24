using FluentAssertions;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.ValueObjects;
using Xunit;
using static GymApp.Tests.Domain.Training.Plans.PlanSamples;

namespace GymApp.Tests.Domain.Training.Plans;

/// <summary>pt.starter_plan: the 3-day starter plan, the staff-unlocked one-week program, and the unlock.</summary>
public class StarterPlanTests
{
    private static readonly Guid Gym = Guid.NewGuid();
    private static readonly Guid Coach = Guid.NewGuid();
    private static readonly Guid Trainee = Guid.NewGuid();
    private static readonly DateOnly Start = new(2026, 11, 24);

    private static PlanDocument Days(int count) =>
        OneWeek(Enumerable.Range(1, count).Select(i => Day($"Day {i}", Exercise("Squat"))).ToArray());

    private static TrainingPlan Limited(TrainingPlanLimit limit, PlanDocument? document = null) =>
        TrainingPlan.Assign(new PlanAssignment(Gym, Coach, Trainee, Start, null, limit), Content(document ?? Days(3)));

    [Theory]
    [InlineData(TrainingPlanLimit.Starter, 3, true)]
    [InlineData(TrainingPlanLimit.Starter, 4, false)]
    [InlineData(TrainingPlanLimit.OneWeek, 7, true)]
    [InlineData(TrainingPlanLimit.None, 7, true)]
    public void Workout_days_per_limit(TrainingPlanLimit limit, int days, bool fits)
    {
        PlanLimitRules.Fits(limit, Days(days)).Should().Be(fits);
    }

    [Theory]
    [InlineData(TrainingPlanLimit.Starter)]
    [InlineData(TrainingPlanLimit.OneWeek)]
    public void A_limited_plan_is_one_week_only(TrainingPlanLimit limit)
    {
        PlanLimitRules.Fits(limit, Days(2).CopyFirstWeekToAll(2)).Should().BeFalse();
    }

    [Theory]
    [InlineData(TrainingPlanLimit.Starter, 26)]
    [InlineData(TrainingPlanLimit.OneWeek, 30)]
    public void A_limited_plan_ends_on_its_last_day(TrainingPlanLimit limit, int lastDay)
    {
        var plan = Limited(limit);

        plan.EndsOn.Should().Be(new DateOnly(2026, 11, lastDay));
        plan.HasEndedOn(new DateOnly(2026, 11, lastDay)).Should().BeFalse();
        plan.HasEndedOn(new DateOnly(2026, 11, lastDay).AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public void Other_plans_never_end_by_date()
    {
        var plan = TrainingPlan.Assign(new PlanAssignment(Gym, Coach, Trainee, Start, null), Content(Days(3)));

        plan.Limit.Should().Be(TrainingPlanLimit.None);
        plan.EndsOn.Should().BeNull();
        plan.HasEndedOn(Start.AddYears(1)).Should().BeFalse();
    }

    [Fact]
    public void A_one_week_program_cannot_be_edited_into_a_longer_one()
    {
        var plan = Limited(TrainingPlanLimit.OneWeek);

        var result = plan.Revise(Content(Days(4).CopyFirstWeekToAll(4)), Coach, expectedVersion: 1);

        result.Error.Should().Be(TrainingPlanErrors.WeekProgramOnly);
        plan.DurationWeeks.Should().Be(1);
    }

    [Fact]
    public void A_starter_plan_can_be_edited_within_three_days()
    {
        var plan = Limited(TrainingPlanLimit.Starter);

        plan.Revise(Content(Days(2)), Coach, expectedVersion: 1).IsSuccess.Should().BeTrue();
        plan.Revise(Content(Days(4)), Coach, expectedVersion: 2).Error.Should().Be(TrainingPlanErrors.StarterPlanOnly);
    }

    [Fact]
    public void Unlocking_and_locking_report_only_real_changes()
    {
        var staff = Guid.NewGuid();
        var unlock = PlanProgramUnlock.Grant(new CoachTrainee(Gym, Coach, Trainee), UnlockLength.OneWeek, staff);

        unlock.Unlock(UnlockLength.OneWeek, staff).Should().BeFalse("already open for a week");
        unlock.Unlock(UnlockLength.ThirtyDays, staff).Should().BeTrue("the staff changed it to 30 days");
        unlock.Lock(Coach).Should().BeTrue("the program used it up");
        unlock.LockedBy.Should().Be(Coach);
        unlock.Lock(staff).Should().BeFalse("already closed");
        unlock.Unlock(UnlockLength.OneWeek, staff).Should().BeTrue();
        unlock.Length.Should().Be(UnlockLength.OneWeek);
        unlock.LockedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(4, 7, true)]
    [InlineData(5, 3, false)]
    public void A_30_day_program_has_up_to_four_weeks(int weeks, int days, bool fits)
    {
        PlanLimitRules.Fits(TrainingPlanLimit.ThirtyDays, Days(days).CopyFirstWeekToAll(weeks)).Should().Be(fits);
    }

    [Fact]
    public void A_30_day_program_ends_after_30_days()
    {
        Limited(TrainingPlanLimit.ThirtyDays).EndsOn.Should().Be(Start.AddDays(29));
        PlanLimitRules.For(UnlockLength.ThirtyDays).Should().Be(TrainingPlanLimit.ThirtyDays);
    }
}
