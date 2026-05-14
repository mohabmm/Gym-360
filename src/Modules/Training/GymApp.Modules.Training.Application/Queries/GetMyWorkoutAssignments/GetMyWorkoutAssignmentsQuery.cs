using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Application.DTOs;

namespace GymApp.Modules.Training.Application.Queries.GetMyWorkoutAssignments;

public sealed record GetMyWorkoutAssignmentsQuery(
    Guid ClientId,
    Guid GymId) : IQuery<List<WorkoutAssignmentDto>>;
