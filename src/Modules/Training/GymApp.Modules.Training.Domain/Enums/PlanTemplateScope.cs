namespace GymApp.Modules.Training.Domain.Enums;

/// <summary>Who can use a plan template: every coach of the gym, or only the coach who made it.</summary>
public enum PlanTemplateScope
{
    Gym = 0,
    Coach = 1
}
