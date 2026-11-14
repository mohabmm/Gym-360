using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Application.DTOs;

public sealed record SessionExerciseLogDto(
    Guid Id,
    Guid SessionId,
    Guid? ExerciseId,
    string ExerciseName,
    string? ExerciseNameAr,
    int OrderIndex,
    int Sets,
    string Reps,
    decimal? Weight,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    ExerciseTarget? Target = null);

public static class SessionExerciseLogMapper
{
    public static SessionExerciseLogDto ToDto(SessionExerciseLog log) => new(
        log.Id, log.SessionId, log.ExerciseId, log.ExerciseName, log.ExerciseNameAr,
        log.OrderIndex, log.Sets, log.Reps, log.Weight, log.Notes, log.CreatedAt, log.UpdatedAt,
        TargetOf(log));

    /// <summary>pt.exercise_progress: the coach's next target, or null when none was set.</summary>
    public static ExerciseTarget? TargetOf(SessionExerciseLog log)
    {
        var target = new ExerciseTarget(log.TargetSets, log.TargetReps, log.TargetWeight);
        return target.IsEmpty ? null : target;
    }
}
