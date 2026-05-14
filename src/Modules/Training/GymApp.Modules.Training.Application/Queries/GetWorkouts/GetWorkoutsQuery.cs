using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Queries.GetWorkouts;

public sealed record GetWorkoutsQuery(
    Guid TrainerId,
    Guid GymId) : IQuery<List<WorkoutDto>>;
