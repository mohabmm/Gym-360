using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.Commands.UpdateWorkout;

internal sealed class UpdateWorkoutCommandHandler : ICommandHandler<UpdateWorkoutCommand>
{
    private readonly IWorkoutRepository _workoutRepository;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public UpdateWorkoutCommandHandler(
        IWorkoutRepository workoutRepository,
        ITrainingUnitOfWork unitOfWork)
    {
        _workoutRepository = workoutRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateWorkoutCommand request, CancellationToken cancellationToken)
    {
        var workout = await _workoutRepository.GetByIdAsync(request.WorkoutId, cancellationToken);

        if (workout is null)
        {
            return Result.Failure(TrainingErrors.WorkoutNotFound);
        }

        workout.Update(new WorkoutDetails
        {
            Name = request.Name ?? workout.Name,
            NameAr = request.NameAr ?? workout.NameAr,
            Description = request.Description,
            EstimatedDurationMinutes = request.EstimatedDuration,
            TargetMuscleGroups = null
        });

        if (request.Exercises is not null)
        {
            // Clear and re-add exercises
            var existingExerciseIds = workout.WorkoutExercises.Select(e => e.Id).ToList();
            foreach (var id in existingExerciseIds)
            {
                workout.RemoveExercise(id);
            }

            foreach (var exercise in request.Exercises)
            {
                workout.AddExercise(new WorkoutExerciseDetails
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
        }

        _workoutRepository.Update(workout);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
