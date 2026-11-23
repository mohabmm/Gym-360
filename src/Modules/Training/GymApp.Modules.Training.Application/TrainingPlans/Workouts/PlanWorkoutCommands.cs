using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Training.Application.TrainingPlans.Workouts;

/// <summary>
/// Saves a plan workout the trainee finished, under the id the device made for it. Safe to repeat:
/// the same <paramref name="ClientId"/> always answers with the workout saved the first time.
/// </summary>
public sealed record SyncPlanWorkoutCommand(PlanActor Actor, Guid ClientId, PlanWorkoutInput Workout) : ICommand<PlanWorkoutSyncDto>;

/// <summary>The trainee's active plan with the next workout; null when the coach has not given one.</summary>
public sealed record GetMyPlanQuery(PlanActor Actor) : IQuery<MyPlanDto?>;

/// <summary>A trainee's plan progress: for themselves, their coach, or the gym's owner/manager.</summary>
public sealed record GetPlanProgressQuery(PlanActor Actor, Guid TraineeId) : IQuery<PlanProgressDto>;
