using GymApp.Modules.Progress.Domain;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;

namespace GymApp.Tests.Domain.Progress;

public sealed class ProgressGoalTests
{
    private static readonly Guid Member = Guid.NewGuid();
    private static readonly Guid Gym = Guid.NewGuid();
    private static readonly Guid Coach = Guid.NewGuid();

    [Fact]
    public void Create_with_goal_and_target_keeps_both_and_who_set_it()
    {
        var result = ProgressGoal.Create(Member, Gym, new ProgressGoalChoice(ProgressGoalType.LoseFat, 80m, Coach));

        result.IsSuccess.Should().BeTrue();
        result.Value.Goal.Should().Be(ProgressGoalType.LoseFat);
        result.Value.TargetWeight.Should().Be(80m);
        result.Value.SetBy.Should().Be(Coach);
        result.Value.UserId.Should().Be(Member);
        result.Value.GymId.Should().Be(Gym);
    }

    [Fact]
    public void Create_without_target_weight_is_allowed()
    {
        var result = ProgressGoal.Create(Member, Gym, new ProgressGoalChoice(ProgressGoalType.GeneralFitness, null, Member));

        result.IsSuccess.Should().BeTrue();
        result.Value.TargetWeight.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(501)]
    public void Create_with_impossible_target_weight_fails(decimal target)
    {
        var result = ProgressGoal.Create(Member, Gym, new ProgressGoalChoice(ProgressGoalType.LoseFat, target, Coach));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProgressErrors.InvalidTargetWeight);
    }

    [Fact]
    public void Change_replaces_goal_and_target_and_records_the_new_setter()
    {
        var goal = ProgressGoal.Create(Member, Gym, new ProgressGoalChoice(ProgressGoalType.LoseFat, 80m, Coach)).Value;

        var changed = goal.Change(new ProgressGoalChoice(ProgressGoalType.BuildMuscle, null, Member));

        changed.IsSuccess.Should().BeTrue();
        goal.Goal.Should().Be(ProgressGoalType.BuildMuscle);
        goal.TargetWeight.Should().BeNull();
        goal.SetBy.Should().Be(Member);
    }

    [Fact]
    public void Failed_change_leaves_the_goal_untouched()
    {
        var goal = ProgressGoal.Create(Member, Gym, new ProgressGoalChoice(ProgressGoalType.LoseFat, 80m, Coach)).Value;

        var changed = goal.Change(new ProgressGoalChoice(ProgressGoalType.BuildMuscle, 0m, Member));

        changed.IsFailure.Should().BeTrue();
        goal.Goal.Should().Be(ProgressGoalType.LoseFat);
        goal.TargetWeight.Should().Be(80m);
    }
}
