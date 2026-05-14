using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Queries.GetTodaysWorkout;

public sealed record GetTodaysWorkoutQuery(
    Guid UserId,
    Guid GymId) : IQuery<TodaysWorkoutResponse?>;
