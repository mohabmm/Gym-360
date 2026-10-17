using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.ValueObjects;

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
            new WorkoutDetails
            {
                Name = workout.Name,
                NameAr = workout.NameAr,
                Description = workout.Description,
                EstimatedDurationMinutes = workout.EstimatedDurationMinutes,
                TargetMuscleGroups = workout.TargetMuscleGroups.ToList()
            },
            isTemplate: true);

        foreach (var exercise in workout.WorkoutExercises)
        {
            template.AddExercise(new WorkoutExerciseDetails
            {
                ExerciseId = exercise.ExerciseId,
                Sets = exercise.Sets,
                Reps = exercise.Reps,
                Weight = exercise.Weight,
                DurationSeconds = exercise.DurationSeconds,
                RestSeconds = exercise.RestSeconds,
                Notes = exercise.Notes,
                SupersetGroupId = exercise.SupersetGroupId
            });
        }

        await _workoutRepository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
