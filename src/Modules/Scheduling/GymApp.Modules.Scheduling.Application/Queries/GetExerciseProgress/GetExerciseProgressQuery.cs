using GymApp.Common.Application.Messaging;
using GymApp.Modules.Scheduling.Application.DTOs;

namespace GymApp.Modules.Scheduling.Application.Queries.GetExerciseProgress;

/// <summary>pt.exercise_progress: the member's board, read by the member or one of their coaches.</summary>
public sealed record GetExerciseProgressQuery(
    Guid CallerId,
    Guid MemberId,
    Guid GymId) : IQuery<ExerciseProgressDto>;
