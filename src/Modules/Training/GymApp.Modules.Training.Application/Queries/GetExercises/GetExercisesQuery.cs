using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Queries.GetExercises;

public sealed record GetExercisesQuery(
    Guid? GymId,
    string? MuscleGroup,
    string? Equipment,
    string? Search,
    int Page,
    int PageSize) : IQuery<PagedResponse<ExerciseDto>>;
