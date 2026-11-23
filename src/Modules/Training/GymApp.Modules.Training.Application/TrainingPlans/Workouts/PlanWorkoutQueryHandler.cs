using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.TrainingPlans.Workouts;

internal sealed class PlanWorkoutQueryHandler
    : IQueryHandler<GetMyPlanQuery, MyPlanDto?>,
      IQueryHandler<GetPlanProgressQuery, PlanProgressDto>
{
    /// <summary>How far back weights and the history go.</summary>
    private const int HistoryDays = 180;
    private const int MaxHistory = 30;

    private readonly PlanAccess _access;
    private readonly ITrainingPlanRepository _plans;
    private readonly IPlanCompletionRepository _completions;

    public PlanWorkoutQueryHandler(PlanAccess access, ITrainingPlanRepository plans, IPlanCompletionRepository completions)
    {
        _access = access;
        _plans = plans;
        _completions = completions;
    }

    public async Task<Result<MyPlanDto?>> Handle(GetMyPlanQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<MyPlanDto?>(enabled.Error);
        var plan = await _plans.GetActiveForTraineeAsync(request.Actor.GymId, request.Actor.UserId, cancellationToken);
        if (plan is null)
            return Result.Success<MyPlanDto?>(null);

        var progress = await LoadAsync(plan.GymId, plan.TraineeId, plan, cancellationToken);
        return Result.Success<MyPlanDto?>(new MyPlanDto(
            plan.ToDto(),
            progress.Cursor!,
            progress.Stats,
            PlanProgress.LastWeights(plan.Document, progress.Recent)));
    }

    public async Task<Result<PlanProgressDto>> Handle(GetPlanProgressQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<PlanProgressDto>(enabled.Error);
        var plans = await _plans.ListForTraineeAsync(request.Actor.GymId, request.TraineeId, cancellationToken);
        if (!await CanSeeAsync(request, plans, cancellationToken))
            return PlanAccess.Forbidden<PlanProgressDto>();

        var active = plans.FirstOrDefault(p => p.IsActive);
        var progress = await LoadAsync(request.Actor.GymId, request.TraineeId, active, cancellationToken);
        return Result.Success(new PlanProgressDto(
            active?.Id,
            progress.Cursor,
            progress.Stats,
            progress.Recent.Take(MaxHistory).Select(c => c.ToDto()).ToList(),
            PlanProgress.Trends(progress.Recent)));
    }

    private sealed record Loaded(PlanCursorDto? Cursor, PlanWeekStats Stats, IReadOnlyList<PlanWorkoutCompletion> Recent);

    private async Task<Loaded> LoadAsync(Guid gymId, Guid traineeId, TrainingPlan? active, CancellationToken cancellationToken)
    {
        var today = TrainingCalendar.Today();
        var recent = await _completions.ListForTraineeAsync(gymId, traineeId, today.AddDays(-HistoryDays), cancellationToken);
        PlanCursorDto? cursor = null;
        if (active is not null)
        {
            // The whole plan, not just the recent window: a long plan must not lose its place.
            var logged = await _completions.ListForPlanAsync(gymId, active.Id, cancellationToken);
            var marks = logged.Where(c => !c.IsVoided).Select(c => c.Mark).ToList();
            cursor = PlanCursor.For(active.Document, marks, today).ToDto();
        }
        return new Loaded(cursor, PlanProgress.Stats(recent, today, active?.DaysPerWeek ?? 0), recent);
    }

    // The trainee, the gym's owner/manager, a coach who gave them a plan, or their current coach.
    private async Task<bool> CanSeeAsync(GetPlanProgressQuery request, IReadOnlyList<TrainingPlan> plans, CancellationToken cancellationToken)
    {
        if (SeesWithoutLookup(request, plans))
            return true;
        return await _access.IsCoachOfAsync(request.Actor, request.TraineeId, cancellationToken);
    }

    private static bool SeesWithoutLookup(GetPlanProgressQuery request, IReadOnlyList<TrainingPlan> plans) =>
        request.Actor.UserId == request.TraineeId || request.Actor.IsGymManager || GavePlan(request.Actor, plans);

    private static bool GavePlan(PlanActor actor, IReadOnlyList<TrainingPlan> plans) =>
        plans.Any(p => p.TrainerId == actor.UserId);
}
