using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.Queries.GetWorkouts;

internal sealed class GetWorkoutsQueryHandler : IQueryHandler<GetWorkoutsQuery, List<WorkoutDto>>
{
    private readonly IWorkoutRepository _workoutRepository;

    public GetWorkoutsQueryHandler(IWorkoutRepository workoutRepository)
    {
        _workoutRepository = workoutRepository;
    }

    public async Task<Result<List<WorkoutDto>>> Handle(
        GetWorkoutsQuery request, CancellationToken cancellationToken)
    {
        var workouts = await _workoutRepository.GetByTrainerAndGymAsync(
            request.TrainerId, request.GymId, cancellationToken);

        var dtos = workouts.Select(w => new WorkoutDto(
            w.Id,
            w.TrainerId,
            w.GymId,
            w.Name,
            w.NameAr,
            w.Description,
            w.EstimatedDurationMinutes,
            w.TargetMuscleGroups.Select(mg => mg.ToString()).ToList(),
            w.IsTemplate,
            w.WorkoutExercises.Count,
            w.CreatedAt,
            w.UpdatedAt)).ToList();

        return Result.Success(dtos);
    }
}
