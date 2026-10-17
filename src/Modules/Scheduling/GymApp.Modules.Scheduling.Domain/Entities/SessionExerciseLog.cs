using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Domain.Entities;

public sealed class SessionExerciseLog : Entity
{
    private SessionExerciseLog() : base() { }

    private SessionExerciseLog(Guid id) : base(id) { }

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

    internal static SessionExerciseLog Create(Guid sessionId, ExerciseLogDetails details)
    {
        return new SessionExerciseLog(Guid.NewGuid())
        {
            SessionId = sessionId,
            ExerciseId = details.ExerciseId,
            ExerciseName = details.ExerciseName,
            ExerciseNameAr = details.ExerciseNameAr,
            OrderIndex = details.OrderIndex,
            Sets = details.Sets,
            Reps = details.Reps,
            Weight = details.Weight,
            Notes = details.Notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    internal void Apply(ExerciseLogDetails details)
    {
        ExerciseId = details.ExerciseId;
        ExerciseName = details.ExerciseName;
        ExerciseNameAr = details.ExerciseNameAr;
        OrderIndex = details.OrderIndex;
        Sets = details.Sets;
        Reps = details.Reps;
        Weight = details.Weight;
        Notes = details.Notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
