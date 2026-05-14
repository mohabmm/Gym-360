using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Queries.GetWorkoutDetail;

public sealed record GetWorkoutDetailQuery(Guid WorkoutId) : IQuery<WorkoutDetailDto>;
