using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Domain.Entities;

public sealed class LogSet : Entity
{
    private LogSet() { }
    private LogSet(Guid id) : base(id) { }

    public Guid WorkoutLogId { get; private set; }
    public Guid ExerciseId { get; private set; }
    public int SetNumber { get; private set; }
    public int Reps { get; private set; }
    public decimal? Weight { get; private set; }
    public WeightUnit WeightUnit { get; private set; }
    public int? DurationSeconds { get; private set; }
    public bool IsCompleted { get; private set; }
    public string? Notes { get; private set; }

    internal static LogSet Create(Guid workoutLogId, LogSetDetails details)
    {
        return new LogSet(Guid.NewGuid())
        {
            WorkoutLogId = workoutLogId,
            ExerciseId = details.ExerciseId,
            SetNumber = details.SetNumber,
            Reps = details.Reps,
            Weight = details.Weight,
            WeightUnit = details.WeightUnit,
            DurationSeconds = details.DurationSeconds,
            IsCompleted = details.IsCompleted,
            Notes = details.Notes
        };
    }
}
