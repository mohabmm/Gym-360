using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Queries.GetWorkoutLogs;

public sealed record GetWorkoutLogsQuery(
    Guid UserId,
    Guid GymId,
    DateOnly? From,
    DateOnly? To) : IQuery<List<WorkoutLogDto>>;
