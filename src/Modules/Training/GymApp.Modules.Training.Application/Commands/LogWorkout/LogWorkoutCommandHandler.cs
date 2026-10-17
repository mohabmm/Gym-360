using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.IntegrationEvents;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.Commands.LogWorkout;

internal sealed class LogWorkoutCommandHandler : ICommandHandler<LogWorkoutCommand, Guid>
{
    private readonly IWorkoutLogRepository _workoutLogRepository;
    private readonly ITrainingUnitOfWork _unitOfWork;
    private readonly IGymPlanService _gymPlanService;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public LogWorkoutCommandHandler(
        IWorkoutLogRepository workoutLogRepository,
        ITrainingUnitOfWork unitOfWork,
        IGymPlanService gymPlanService,
        IIntegrationEventPublisher integrationEvents)
    {
        _workoutLogRepository = workoutLogRepository;
        _unitOfWork = unitOfWork;
        _gymPlanService = gymPlanService;
        _integrationEvents = integrationEvents;
    }

    public async Task<Result<Guid>> Handle(LogWorkoutCommand request, CancellationToken cancellationToken)
    {
        var isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "session_logging", cancellationToken);

        if (!isEnabled)
        {
            var currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable(
                "session_logging", currentPlan, "Pro"));
        }

        // Idempotent on (UserId, Date, WorkoutId) — the DB enforces this tuple
        // unique. A retry (double-tap / re-submit) returns the existing log's
        // id so the client sees success and can move on.
        var existing = await _workoutLogRepository.GetByUserAndDateAsync(
            request.UserId, request.Date, request.WorkoutId, cancellationToken);

        if (existing is not null)
        {
            return Result.Success(existing.Id);
        }

        var workoutLog = WorkoutLog.Create(
            request.UserId,
            request.GymId,
            new WorkoutLogDetails
            {
                Date = request.Date,
                StartedAt = request.StartedAt,
                WorkoutId = request.WorkoutId,
                Notes = request.Notes,
                PerceivedExertion = request.PerceivedExertion
            });

        foreach (var setDto in request.Sets)
        {
            var weightUnit = Enum.TryParse<WeightUnit>(setDto.WeightUnit, ignoreCase: true, out var unit)
                ? unit
                : WeightUnit.Kg;

            workoutLog.AddSet(new LogSetDetails
            {
                ExerciseId = setDto.ExerciseId,
                SetNumber = setDto.SetNumber,
                Reps = setDto.Reps,
                Weight = setDto.Weight,
                WeightUnit = weightUnit,
                DurationSeconds = setDto.DurationSeconds,
                IsCompleted = setDto.IsCompleted,
                Notes = setDto.Notes
            });
        }

        if (request.CompletedAt.HasValue)
        {
            workoutLog.Complete(request.CompletedAt.Value, request.DurationMinutes);
        }

        await _workoutLogRepository.AddAsync(workoutLog, cancellationToken);

        // Progress keeps the daily workout streak (home stats + weekly activity)
        // off this event. LoggedAt carries the client's workout date rather than
        // the UTC clock so a late-evening Cairo workout counts for the right day.
        _integrationEvents.Enqueue(new WorkoutLoggedIntegrationEvent(
            request.UserId,
            request.GymId,
            request.Date.ToDateTime(TimeOnly.MinValue)));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(workoutLog.Id);
    }
}
