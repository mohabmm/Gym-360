namespace GymApp.Modules.Progress.Domain.Enums;

/// <summary>What the member is training for. Decides whether a change counts as progress.</summary>
public enum ProgressGoalType
{
    LoseFat = 0,
    BuildMuscle = 1,
    GeneralFitness = 2
}
