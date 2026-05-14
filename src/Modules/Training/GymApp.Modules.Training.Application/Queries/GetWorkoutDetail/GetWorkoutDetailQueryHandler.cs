using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.Queries.GetWorkoutDetail;

internal sealed class GetWorkoutDetailQueryHandler : IQueryHandler<GetWorkoutDetailQuery, WorkoutDetailDto>
{
    private readonly IWorkoutRepository _workoutRepository;

    public GetWorkoutDetailQueryHandler(IWorkoutRepository workoutRepository)
    {
        _workoutRepository = workoutRepository;
    }

    public async Task<Result<WorkoutDetailDto>> Handle(
        GetWorkoutDetailQuery request, CancellationToken cancellationToken)
    {
        var workout = await _workoutRepository.GetByIdAsync(request.WorkoutId, cancellationToken);

        if (workout is null)
        {
            return Result.Failure<WorkoutDetailDto>(TrainingErrors.WorkoutNotFound);
        }

        var exercises = workout.WorkoutExercises
            .OrderBy(e => e.Order)
            .Select(e => new WorkoutExerciseDetailDto(
                e.Id,
                e.ExerciseId,
                e.Order,
                e.Sets,
                e.Reps,
                e.Weight,
                e.DurationSeconds,
                e.RestSeconds,
                e.Notes,
                e.SupersetGroupId))
            .ToList();

        var dto = new WorkoutDetailDto(
            workout.Id,
            workout.TrainerId,
            workout.GymId,
            workout.Name,
            workout.NameAr,
            workout.Description,
            workout.EstimatedDurationMinutes,
            workout.TargetMuscleGroups.Select(mg => mg.ToString()).ToList(),
            workout.IsTemplate,
            exercises,
            workout.CreatedAt,
            workout.UpdatedAt);

        return Result.Success(dto);
    }
}
