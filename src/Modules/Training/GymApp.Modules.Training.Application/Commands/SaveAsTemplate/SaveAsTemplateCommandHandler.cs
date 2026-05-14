using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;

namespace GymApp.Modules.Training.Application.Commands.SaveAsTemplate;

internal sealed class SaveAsTemplateCommandHandler : ICommandHandler<SaveAsTemplateCommand>
{
    private readonly IWorkoutRepository _workoutRepository;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public SaveAsTemplateCommandHandler(
        IWorkoutRepository workoutRepository,
        ITrainingUnitOfWork unitOfWork)
    {
        _workoutRepository = workoutRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SaveAsTemplateCommand request, CancellationToken cancellationToken)
    {
        var workout = await _workoutRepository.GetByIdAsync(request.WorkoutId, cancellationToken);

        if (workout is null)
        {
            return Result.Failure(TrainingErrors.WorkoutNotFound);
        }

        // Create a template copy of the workout
        var template = Workout.Create(
            workout.TrainerId,
            workout.GymId,
            workout.Name,
            workout.NameAr,
            workout.Description,
            workout.EstimatedDurationMinutes,
            workout.TargetMuscleGroups.ToList(),
            isTemplate: true);

        foreach (var exercise in workout.WorkoutExercises)
        {
            template.AddExercise(
                exercise.ExerciseId,
                exercise.Sets,
                exercise.Reps,
                exercise.Weight,
                exercise.DurationSeconds,
                exercise.RestSeconds,
                exercise.Notes,
                exercise.SupersetGroupId);
        }

        await _workoutRepository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
