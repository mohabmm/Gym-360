using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Training.Application.TrainingPlans.Workouts;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.TrainingPlans.Coaching;

/// <summary>
/// pt.training_plan, part D: the coach's clients at a glance — their plan, workouts this week,
/// last workout — with those inactive for the gym's "inactive after" days first.
/// </summary>
internal sealed class CoachPlanOverviewHandler : IQueryHandler<GetCoachPlanOverviewQuery, CoachPlanOverviewDto>
{
    private readonly PlanAccess _access;
    private readonly ITrainerAssignmentLookup _assignments;
    private readonly ITrainingPlanRepository _plans;
    private readonly IPlanCompletionRepository _completions;
    private readonly ITrainingPlanSettingsRepository _settings;

    public CoachPlanOverviewHandler(
        PlanAccess access,
        ITrainerAssignmentLookup assignments,
        ITrainingPlanRepository plans,
        IPlanCompletionRepository completions,
        ITrainingPlanSettingsRepository settings)
    {
        _access = access;
        _assignments = assignments;
        _plans = plans;
        _completions = completions;
        _settings = settings;
    }

    public async Task<Result<CoachPlanOverviewDto>> Handle(GetCoachPlanOverviewQuery request, CancellationToken cancellationToken)
    {
        var gymId = request.Actor.GymId;
        var enabled = await _access.EnsureEnabledAsync(gymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<CoachPlanOverviewDto>(enabled.Error);

        var clients = await _assignments.GetClientsForTrainerAsync(request.Actor.UserId, gymId, cancellationToken);
        var inactiveDays = (await _settings.GetAsync(gymId, cancellationToken))?.InactiveDays ?? TrainingPlanSettings.DefaultInactiveDays;
        var ids = clients.ToList();
        var plans = (await _plans.ListActiveForTraineesAsync(gymId, ids, cancellationToken)).ToDictionary(p => p.TraineeId);
        var today = TrainingCalendar.Today();
        var workouts = (await _completions.ListForTraineesAsync(gymId, ids, Since(plans.Values, today), cancellationToken))
            .ToLookup(w => w.TraineeId);

        var rows = ids.Select(id => plans.TryGetValue(id, out var plan)
                ? Row(plan, workouts[id].ToList(), today, inactiveDays)
                : new CoachClientPlanDto { TraineeId = id })
            .OrderByDescending(r => r.IsInactive)
            .ThenByDescending(r => r.PlanId.HasValue)
            .ThenByDescending(r => r.DaysSince ?? -1)
            .ToList();
        return Result.Success(new CoachPlanOverviewDto(inactiveDays, rows));
    }

    /// <summary>Far enough back for every active plan's cursor and this month's count.</summary>
    private static DateOnly Since(IEnumerable<TrainingPlan> plans, DateOnly today)
    {
        var earliest = new[] { PlanCalendar.MonthStart(today), PlanCalendar.WeekStart(today) }
            .Concat(plans.Select(p => p.StartDate))
            .Min();
        return earliest;
    }

    private static CoachClientPlanDto Row(TrainingPlan plan, IReadOnlyList<PlanWorkoutCompletion> workouts, DateOnly today, int inactiveDays)
    {
        var marks = workouts.Where(w => w.PlanId == plan.Id && !w.IsVoided).Select(w => w.Mark).ToList();
        var cursor = PlanCursor.For(plan.Document, marks, today);
        var stats = PlanProgress.Stats(workouts, today, plan.DaysPerWeek);
        var activity = PlanActivity.For(plan, workouts, today, inactiveDays);
        return new CoachClientPlanDto
        {
            TraineeId = plan.TraineeId,
            PlanId = plan.Id,
            PlanName = plan.Name,
            PlanNameAr = plan.NameAr,
            ThisWeek = stats.ThisWeek,
            WeekTarget = stats.WeekTarget,
            DoneCount = cursor.LastDoneIndex + 1,
            Total = cursor.Total,
            LastWorkoutOn = activity.LastWorkoutOn,
            DaysSince = activity.DaysSince,
            IsInactive = activity.IsInactive,
            NextDayTitle = cursor.Next?.Day.Title,
            NextDayTitleAr = cursor.Next?.Day.TitleAr
        };
    }
}
