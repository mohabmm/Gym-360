using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.Queries.GetTemplates;

internal sealed class GetTemplatesQueryHandler : IQueryHandler<GetTemplatesQuery, List<WorkoutDto>>
{
    private readonly IWorkoutRepository _workoutRepository;

    public GetTemplatesQueryHandler(IWorkoutRepository workoutRepository)
    {
        _workoutRepository = workoutRepository;
    }

    public async Task<Result<List<WorkoutDto>>> Handle(
        GetTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _workoutRepository.GetTemplatesAsync(
            request.GymId, cancellationToken);

        var dtos = templates.Select(w => new WorkoutDto(
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
