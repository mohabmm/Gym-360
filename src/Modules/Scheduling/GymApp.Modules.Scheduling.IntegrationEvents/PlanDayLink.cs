namespace GymApp.Modules.Scheduling.IntegrationEvents;

/// <summary>
/// pt.training_plan: the plan day the coach did with the trainee in this session (optional).
/// Scheduling only carries it; Training records the plan workout.
/// </summary>
/// <param name="SessionDate">Gym-local day the session happened.</param>
public sealed record PlanDayLink(Guid PlanId, string DayKey, DateOnly SessionDate);
