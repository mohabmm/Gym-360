using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.Domain.Entities;

public sealed class SessionExerciseLog : Entity
{
    private SessionExerciseLog() : base() { }

    private SessionExerciseLog(
        Guid id,
        Guid sessionId,
        Guid? exerciseId,
        string exerciseName,
        string? exerciseNameAr,
        int orderIndex,
        int sets,
        string reps,
        decimal? weight,
        string? notes) : base(id)
    {
        SessionId = sessionId;
        ExerciseId = exerciseId;
        ExerciseName = exerciseName;
        ExerciseNameAr = exerciseNameAr;
        OrderIndex = orderIndex;
        Sets = sets;
        Reps = reps;
        Weight = weight;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid SessionId { get; private set; }
    public Guid? ExerciseId { get; private set; }
    public string ExerciseName { get; private set; } = default!;
    public string? ExerciseNameAr { get; private set; }
    public int OrderIndex { get; private set; }
    public int Sets { get; private set; }
    public string Reps { get; private set; } = default!;
    public decimal? Weight { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    internal static SessionExerciseLog Create(
        Guid sessionId,
        Guid? exerciseId,
        string exerciseName,
        string? exerciseNameAr,
        int orderIndex,
        int sets,
        string reps,
        decimal? weight,
        string? notes)
    {
        return new SessionExerciseLog(
            Guid.NewGuid(),
            sessionId,
            exerciseId,
            exerciseName,
            exerciseNameAr,
            orderIndex,
            sets,
            reps,
            weight,
            notes);
    }

    internal void Apply(
        Guid? exerciseId,
        string exerciseName,
        string? exerciseNameAr,
        int orderIndex,
        int sets,
        string reps,
        decimal? weight,
        string? notes)
    {
        ExerciseId = exerciseId;
        ExerciseName = exerciseName;
        ExerciseNameAr = exerciseNameAr;
        OrderIndex = orderIndex;
        Sets = sets;
        Reps = reps;
        Weight = weight;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
