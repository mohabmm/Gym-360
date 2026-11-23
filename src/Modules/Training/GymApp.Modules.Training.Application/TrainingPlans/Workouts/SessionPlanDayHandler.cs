using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Domain.ValueObjects;
using GymApp.Modules.Scheduling.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Training.Application.TrainingPlans.Workouts;

/// <summary>
/// pt.training_plan, part C: a PT session the coach logged against a plan day counts as that
/// plan workout for the trainee; if the desk reverses the session, the workout stops counting.
/// The session flow itself (deduction, trainee confirmation, dispute) is untouched. A dispute
/// alone voids nothing: the desk may still keep the session, and only a reversal undoes it.
/// </summary>
internal sealed class SessionPlanDayHandler
    : INotificationHandler<SessionCompletedIntegrationEvent>,
      INotificationHandler<SessionReversedIntegrationEvent>
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly ITrainingPlanRepository _plans;
    private readonly IPlanCompletionRepository _completions;
    private readonly ITrainingUnitOfWork _unitOfWork;
    private readonly ILogger<SessionPlanDayHandler> _logger;

    public SessionPlanDayHandler(
        IFeatureFlagService featureFlags,
        ITrainingPlanRepository plans,
        IPlanCompletionRepository completions,
        ITrainingUnitOfWork unitOfWork,
        ILogger<SessionPlanDayHandler> logger)
    {
        _featureFlags = featureFlags;
        _plans = plans;
        _completions = completions;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(SessionCompletedIntegrationEvent e, CancellationToken cancellationToken)
    {
        if (e.PlanDay is not { } link)
            return;
        if (!await _featureFlags.IsEnabledAsync(e.GymId, FeatureKeys.PtTrainingPlan, cancellationToken))
            return;

        var plan = await _plans.GetByIdAsync(e.GymId, link.PlanId, cancellationToken);
        if (SlotFor(plan, e.MemberId, link.DayKey) is not { } slot)
        {
            _logger.LogWarning("Session {SessionId}: plan day {DayKey} of plan {PlanId} not found for this trainee; not recorded",
                e.SessionId, link.DayKey, link.PlanId);
            return;
        }
        if (await AlreadyCountedAsync(plan!, link, e.SessionId, cancellationToken))
            return;

        await _completions.TryAddAsync(PlanWorkoutCompletion.Record(plan!, slot, LogOf(e.SessionId, link)), cancellationToken);
    }

    /// <summary>The day, when the plan exists and belongs to the session's trainee.</summary>
    private static PlanSlot? SlotFor(TrainingPlan? plan, Guid memberId, string dayKey) =>
        plan?.TraineeId == memberId ? PlanCursor.Find(plan.Document, dayKey) : null;

    public async Task Handle(SessionReversedIntegrationEvent e, CancellationToken cancellationToken)
    {
        var linked = await _completions.ListForSessionAsync(e.GymId, e.SessionId, cancellationToken);
        if (linked.Count == 0)
            return;
        foreach (var completion in linked)
            completion.Void();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // The session id is the upload id, so a redelivered event is a no-op; and if the trainee
    // already ticked the same day off in the app that date, it is one workout, not two.
    private async Task<bool> AlreadyCountedAsync(TrainingPlan plan, PlanDayLink link, Guid sessionId, CancellationToken cancellationToken)
    {
        if (await _completions.GetByClientIdAsync(plan.TraineeId, sessionId, cancellationToken) is not null)
            return true;
        var logged = await _completions.ListForPlanAsync(plan.GymId, plan.Id, cancellationToken);
        return logged.Any(c => !c.IsVoided && c.DayKey == link.DayKey && c.CompletedOn == link.SessionDate);
    }

    private static PlanWorkoutLog LogOf(Guid sessionId, PlanDayLink link)
    {
        var today = TrainingCalendar.Today();
        return new PlanWorkoutLog
        {
            ClientId = sessionId,
            CompletedAt = link.SessionDate == today ? DateTime.UtcNow : TrainingCalendar.NoonUtc(link.SessionDate),
            CompletedOn = link.SessionDate,
            Source = PlanCompletionSource.CoachSession,
            SessionId = sessionId
        };
    }
}
