using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;

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

        workout.Update(
            request.Name ?? workout.Name,
            request.NameAr ?? workout.NameAr,
            request.Description,
            request.EstimatedDuration,
            targetMuscleGroups: null);

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
        }

        _workoutRepository.Update(workout);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
