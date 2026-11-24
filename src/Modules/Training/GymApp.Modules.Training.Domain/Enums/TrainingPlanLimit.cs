namespace GymApp.Modules.Training.Domain.Enums;

/// <summary>
/// pt.starter_plan: how far a coach may go with a member's plan.
/// <see cref="Starter"/> — the first 3-day plan, before the staff unlock anything.
/// <see cref="OneWeek"/> / <see cref="ThirtyDays"/> — after a staff unlock of that length: a plan
/// from a template or ready program (one plan per unlock).
/// <see cref="None"/> — the flag is off: any plan, never ends by date.
/// </summary>
public enum TrainingPlanLimit
{
    None = 0,
    Starter = 1,
    OneWeek = 2,
    ThirtyDays = 3
}
