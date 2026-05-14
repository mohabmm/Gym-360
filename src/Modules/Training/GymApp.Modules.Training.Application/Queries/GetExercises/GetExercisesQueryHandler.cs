using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.Queries.GetExercises;

internal sealed class GetExercisesQueryHandler : IQueryHandler<GetExercisesQuery, PagedResponse<ExerciseDto>>
{
    private readonly IExerciseRepository _exerciseRepository;

    public GetExercisesQueryHandler(IExerciseRepository exerciseRepository)
    {
        _exerciseRepository = exerciseRepository;
    }

    public async Task<Result<PagedResponse<ExerciseDto>>> Handle(
        GetExercisesQuery request, CancellationToken cancellationToken)
    {
        MuscleGroup? muscleGroup = !string.IsNullOrEmpty(request.MuscleGroup)
            ? Enum.Parse<MuscleGroup>(request.MuscleGroup, ignoreCase: true)
            : null;

        Equipment? equipment = !string.IsNullOrEmpty(request.Equipment)
            ? Enum.Parse<Equipment>(request.Equipment, ignoreCase: true)
            : null;

        var exercises = await _exerciseRepository.GetAllAsync(
            request.GymId,
            muscleGroup: muscleGroup,
            equipment: equipment,
            searchTerm: request.Search,
            cancellationToken: cancellationToken);

        var totalCount = exercises.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var pagedExercises = exercises
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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

        var response = new PagedResponse<ExerciseDto>(
            pagedExercises, request.Page, request.PageSize, totalCount, totalPages);

        return Result.Success(response);
    }
}
