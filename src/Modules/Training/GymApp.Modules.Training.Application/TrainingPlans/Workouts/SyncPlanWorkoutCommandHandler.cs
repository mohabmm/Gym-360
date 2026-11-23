using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.TrainingPlans.Workouts;

/// <summary>
/// Offline-safe upload of a finished plan workout. A real workout is never thrown away for being
/// late: a retry returns the first save, the same day on the same date is kept once, and a
/// workout for a day the coach since removed (or for a replaced plan) is still saved.
/// </summary>
internal sealed class SyncPlanWorkoutCommandHandler : ICommandHandler<SyncPlanWorkoutCommand, PlanWorkoutSyncDto>
{
    /// <summary>Phone clocks drift; anything further ahead than this is taken as "now".</summary>
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(5);

    private readonly PlanAccess _access;
    private readonly ITrainingPlanRepository _plans;
    private readonly IPlanCompletionRepository _completions;

    public SyncPlanWorkoutCommandHandler(PlanAccess access, ITrainingPlanRepository plans, IPlanCompletionRepository completions)
    {
        _access = access;
        _plans = plans;
        _completions = completions;
    }

    public async Task<Result<PlanWorkoutSyncDto>> Handle(SyncPlanWorkoutCommand request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<PlanWorkoutSyncDto>(enabled.Error);

        var saved = await _completions.GetByClientIdAsync(request.Actor.UserId, request.ClientId, cancellationToken);
        if (saved is not null)
            return Synced(saved, PlanWorkoutOutcome.AlreadySynced);

        var plan = await _plans.GetByIdAsync(request.Actor.GymId, request.Workout.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<PlanWorkoutSyncDto>(TrainingPlanErrors.PlanNotFound);
        if (plan.TraineeId != request.Actor.UserId)
            return PlanAccess.Forbidden<PlanWorkoutSyncDto>();

        var log = ToLog(request);
        var valid = PlanWorkoutLogRules.Validate(log);
        if (valid.IsFailure)
            return Result.Failure<PlanWorkoutSyncDto>(valid.Error);
        return await RecordAsync(plan, request.Workout, log, cancellationToken);
    }

    private async Task<Result<PlanWorkoutSyncDto>> RecordAsync(
        TrainingPlan plan, PlanWorkoutInput input, PlanWorkoutLog log, CancellationToken cancellationToken)
    {
        var current = PlanCursor.Find(plan.Document, input.DayKey);
        var slot = current ?? SnapshotSlot(input);
        if (slot is null)
            return Result.Failure<PlanWorkoutSyncDto>(TrainingPlanErrors.DayNotInPlan);

        var logged = await _completions.ListForPlanAsync(plan.GymId, plan.Id, cancellationToken);
        var sameDay = logged.FirstOrDefault(c => !c.IsVoided && c.DayKey == input.DayKey && c.CompletedOn == log.CompletedOn);
        if (sameDay is not null)
            return Synced(sameDay, PlanWorkoutOutcome.Duplicate);

        var completion = PlanWorkoutCompletion.Record(plan, slot, log);
        if (!await _completions.TryAddAsync(completion, cancellationToken))
        {
            // Another upload of the same workout won the race.
            var winner = await _completions.GetByClientIdAsync(plan.TraineeId, log.ClientId, cancellationToken);
            return winner is null ? Result.Failure<PlanWorkoutSyncDto>(TrainingPlanErrors.VersionConflict) : Synced(winner, PlanWorkoutOutcome.AlreadySynced);
        }
        return Synced(completion, OutcomeFor(plan, current));
    }

    private static string OutcomeFor(TrainingPlan plan, PlanSlot? current)
    {
        if (!plan.IsActive)
            return PlanWorkoutOutcome.PlanReplaced;
        return current is null ? PlanWorkoutOutcome.PlanChanged : PlanWorkoutOutcome.Created;
    }

    /// <summary>The day as the app saw it, for a day that is no longer in the plan.</summary>
    private static PlanSlot? SnapshotSlot(PlanWorkoutInput input)
    {
        if (string.IsNullOrWhiteSpace(input.DayTitle) || input.DayKey.Length > 50)
            return null;
        var day = new PlanDay { Key = input.DayKey, Title = input.DayTitle.Trim(), TitleAr = input.DayTitleAr.OrNull(), Exercises = Array.Empty<PlanExercise>() };
        return new PlanSlot(Math.Max(input.SequenceIndex ?? 0, 0), Math.Max(input.Week ?? 1, 1), day);
    }

    private static PlanWorkoutLog ToLog(SyncPlanWorkoutCommand request)
    {
        var completedAt = DateTime.SpecifyKind(request.Workout.CompletedAt.ToUniversalTime(), DateTimeKind.Utc);
        var now = DateTime.UtcNow;
        if (completedAt > now + ClockSkew)
            completedAt = now;
        return new PlanWorkoutLog
        {
            ClientId = request.ClientId,
            CompletedAt = completedAt,
            CompletedOn = TrainingCalendar.LocalDate(completedAt),
            Sets = request.Workout.Sets ?? Array.Empty<CompletedSet>(),
            DurationMinutes = request.Workout.DurationMinutes,
            Note = request.Workout.Note
        };
    }

    private static Result<PlanWorkoutSyncDto> Synced(PlanWorkoutCompletion completion, string outcome) =>
        Result.Success(new PlanWorkoutSyncDto(completion.ToDto(), outcome));
}
