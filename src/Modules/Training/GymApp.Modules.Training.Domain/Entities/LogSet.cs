using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;

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

    internal static LogSet Create(
        Guid workoutLogId,
        Guid exerciseId,
        int setNumber,
        int reps,
        decimal? weight = null,
        WeightUnit weightUnit = WeightUnit.Kg,
        int? durationSeconds = null,
        bool isCompleted = true,
        string? notes = null)
    {
        return new LogSet(Guid.NewGuid())
        {
            WorkoutLogId = workoutLogId,
            ExerciseId = exerciseId,
            SetNumber = setNumber,
            Reps = reps,
            Weight = weight,
            WeightUnit = weightUnit,
            DurationSeconds = durationSeconds,
            IsCompleted = isCompleted,
            Notes = notes
        };
    }
}
