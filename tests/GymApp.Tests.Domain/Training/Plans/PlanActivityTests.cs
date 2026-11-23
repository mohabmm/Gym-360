using FluentAssertions;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.ValueObjects;
using Xunit;
using static GymApp.Tests.Domain.Training.Plans.PlanSamples;

namespace GymApp.Tests.Domain.Training.Plans;

public class PlanActivityTests
{
    private static readonly DateOnly Today = new(2026, 11, 25);

    private static TrainingPlan PlanStarting(DateOnly start) => TrainingPlan.Assign(
        new PlanAssignment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), start, null), Content());

    private static PlanWorkoutCompletion DoneOn(TrainingPlan plan, DateOnly on) =>
        PlanWorkoutCompletion.Record(plan, PlanCursor.Slots(plan.Document)[0], new PlanWorkoutLog
        {
            ClientId = Guid.NewGuid(), CompletedAt = on.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), CompletedOn = on
        });

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(9, true)]
    public void A_trainee_is_inactive_after_the_gym_s_days_without_a_workout(int daysAgo, bool inactive)
    {
        var plan = PlanStarting(Today.AddDays(-20));

        var activity = PlanActivity.For(plan, new[] { DoneOn(plan, Today.AddDays(-daysAgo)) }, Today, inactiveDays: 5);

        activity.IsInactive.Should().Be(inactive);
        activity.DaysSince.Should().Be(daysAgo);
    }

    [Fact]
    public void With_no_workout_yet_the_days_count_from_the_plan_start_and_a_future_start_is_never_inactive()
    {
        PlanActivity.For(PlanStarting(Today.AddDays(-6)), Array.Empty<PlanWorkoutCompletion>(), Today, 5)
            .Should().Be(new PlanActivity(null, 6, true));
        PlanActivity.For(PlanStarting(Today.AddDays(2)), Array.Empty<PlanWorkoutCompletion>(), Today, 5)
            .IsInactive.Should().BeFalse();
    }

    [Fact]
    public void A_voided_workout_does_not_count_as_activity()
    {
        var plan = PlanStarting(Today.AddDays(-10));
        var voided = DoneOn(plan, Today);
        voided.Void();

        PlanActivity.For(plan, new[] { voided }, Today, 5).DaysSince.Should().Be(10);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void Inactive_days_are_kept_between_two_and_thirty(int days, bool ok)
    {
        var settings = TrainingPlanSettings.ForGym(Guid.NewGuid());

        settings.SetInactiveDays(days, Guid.NewGuid()).IsSuccess.Should().Be(ok);
        settings.InactiveDays.Should().Be(ok ? days : TrainingPlanSettings.DefaultInactiveDays);
    }
}
