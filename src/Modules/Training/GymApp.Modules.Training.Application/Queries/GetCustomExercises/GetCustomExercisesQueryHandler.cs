using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.Queries.GetCustomExercises;

internal sealed class GetCustomExercisesQueryHandler
    : IQueryHandler<GetCustomExercisesQuery, List<ExerciseDto>>
{
    private readonly IExerciseRepository _exerciseRepository;

    public GetCustomExercisesQueryHandler(IExerciseRepository exerciseRepository)
    {
        _exerciseRepository = exerciseRepository;
    }

    public async Task<Result<List<ExerciseDto>>> Handle(
        GetCustomExercisesQuery request, CancellationToken cancellationToken)
    {
        var exercises = await _exerciseRepository.GetAllAsync(
            request.GymId, cancellationToken: cancellationToken);

        var customExercises = exercises
            .Where(e => e.Scope == ExerciseScope.GymSpecific && e.GymId == request.GymId)
            .Select(e => new ExerciseDto(
                e.Id,
                e.Name,
                e.NameAr,
                e.Description,
                e.Instructions,
                e.DemoMediaUrl,
                e.MuscleGroups.Select(mg => mg.ToString()).ToList(),
                e.Equipment.ToString(),
                e.Category.ToString(),
                e.Scope.ToString(),
                e.GymId,
                e.CreatedBy,
                e.IsActive))
            .ToList();

        return Result.Success(customExercises);
    }
}
