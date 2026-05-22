using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;

namespace GymApp.Modules.Training.Application.Commands.CreateWorkout;

internal sealed class CreateWorkoutCommandHandler : ICommandHandler<CreateWorkoutCommand, Guid>
{
    private readonly IWorkoutRepository _workoutRepository;
    private readonly ITrainingUnitOfWork _unitOfWork;
    private readonly IGymPlanService _gymPlanService;

    public CreateWorkoutCommandHandler(
        IWorkoutRepository workoutRepository,
        ITrainingUnitOfWork unitOfWork,
        IGymPlanService gymPlanService)
    {
        _workoutRepository = workoutRepository;
        _unitOfWork = unitOfWork;
        _gymPlanService = gymPlanService;
    }

    public async Task<Result<Guid>> Handle(CreateWorkoutCommand request, CancellationToken cancellationToken)
    {
        var isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "workout_builder", cancellationToken);

        if (!isEnabled)
        {
            var currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable(
                "workout_builder", currentPlan, "Pro"));
        }

        var workout = Workout.Create(
            request.TrainerId,
            request.GymId,
            request.Name,
            request.NameAr,
            request.Description,
            request.EstimatedDuration,
            isTemplate: request.IsTemplate);

        foreach (var exercise in request.Exercises)
        {
            workout.AddExercise(
                exercise.ExerciseId,
                exercise.Sets,
                exercise.Reps,
                exercise.Weight,
                exercise.DurationSeconds,
                exercise.RestSeconds,
                exercise.Notes,
                exercise.SupersetGroupId);
        }

        await _workoutRepository.AddAsync(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(workout.Id);
    }
}
