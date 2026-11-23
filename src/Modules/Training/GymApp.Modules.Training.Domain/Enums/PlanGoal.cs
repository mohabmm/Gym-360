namespace GymApp.Modules.Training.Domain.Enums;

/// <summary>pt.training_plan: what a plan aims at. <see cref="Other"/> carries free text.</summary>
public enum PlanGoal
{
    GeneralFitness = 0,
    FatLoss = 1,
    MuscleGain = 2,
    Other = 3
}
