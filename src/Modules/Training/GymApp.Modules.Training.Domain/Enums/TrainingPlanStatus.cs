namespace GymApp.Modules.Training.Domain.Enums;

/// <summary>A trainee has at most one <see cref="Active"/> plan; older ones stay as history.</summary>
public enum TrainingPlanStatus
{
    Active = 0,
    Replaced = 1,
    Completed = 2,
    Cancelled = 3
}
