using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Queries.GetCustomExercises;

public sealed record GetCustomExercisesQuery(Guid GymId) : IQuery<List<ExerciseDto>>;
