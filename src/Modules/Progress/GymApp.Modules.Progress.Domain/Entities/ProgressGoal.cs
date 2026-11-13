using GymApp.Common.Domain;
using GymApp.Modules.Progress.Domain.Enums;

namespace GymApp.Modules.Progress.Domain.Entities;

/// <summary>
/// pt.progress_measurements: the member's goal at one gym (one row per member and gym).
/// Set by the coach on the first measurement or by the member; the apps color changes by it.
/// </summary>
public sealed class ProgressGoal : Entity
{
    private const decimal MaxTargetWeight = 500m;

    private ProgressGoal() { }

    private ProgressGoal(Guid id, Guid userId, Guid gymId) : base(id)
    {
        UserId = userId;
        GymId = gymId;
    }

    public Guid UserId { get; private set; }
    public Guid GymId { get; private set; }
    public ProgressGoalType Goal { get; private set; }
    public decimal? TargetWeight { get; private set; }
    public Guid SetBy { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Result<ProgressGoal> Create(Guid userId, Guid gymId, ProgressGoalChoice choice)
    {
        var goal = new ProgressGoal(Guid.NewGuid(), userId, gymId);
        var applied = goal.Change(choice);
        return applied.IsSuccess ? Result.Success(goal) : Result.Failure<ProgressGoal>(applied.Error);
    }

    public Result Change(ProgressGoalChoice choice)
    {
        if (choice.TargetWeight is <= 0 or > MaxTargetWeight)
            return Result.Failure(ProgressErrors.InvalidTargetWeight);

        Goal = choice.Goal;
        TargetWeight = choice.TargetWeight;
        SetBy = choice.SetBy;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}

/// <summary>A goal, optional target weight (kg) and who chose it.</summary>
public sealed record ProgressGoalChoice(ProgressGoalType Goal, decimal? TargetWeight, Guid SetBy);
