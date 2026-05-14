using GymApp.Common.Application.Messaging;
using GymApp.Modules.Scheduling.Application.DTOs;

namespace GymApp.Modules.Scheduling.Application.Commands.LogSessionExercises;

public sealed record ExerciseLogItem(
    Guid? ExerciseId,
    string ExerciseName,
    string? ExerciseNameAr,
    int OrderIndex,
    int Sets,
    string Reps,
    decimal? Weight,
    string? Notes);

public sealed record LogSessionExercisesCommand(
    Guid SessionId,
    Guid TrainerId,
    IReadOnlyList<ExerciseLogItem> Items) : ICommand<IReadOnlyList<SessionExerciseLogDto>>;
