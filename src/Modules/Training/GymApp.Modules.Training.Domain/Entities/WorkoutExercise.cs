using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Domain.Entities;

public sealed class WorkoutExercise : Entity
{
    private WorkoutExercise() { }
    private WorkoutExercise(Guid id) : base(id) { }

    public Guid WorkoutId { get; private set; }
    public Guid ExerciseId { get; private set; }
    public int Order { get; private set; }
    public int Sets { get; private set; }
    public string Reps { get; private set; } = default!;
    public decimal? Weight { get; private set; }
    public int? DurationSeconds { get; private set; }
    public int? RestSeconds { get; private set; }
    public string? Notes { get; private set; }
    public Guid? SupersetGroupId { get; private set; }

    internal static WorkoutExercise Create(Guid workoutId, int order, WorkoutExerciseDetails details)
    {
        return new WorkoutExercise(Guid.NewGuid())
        {
            WorkoutId = workoutId,
            ExerciseId = details.ExerciseId,
            Order = order,
            Sets = details.Sets,
            Reps = details.Reps,
            Weight = details.Weight,
            DurationSeconds = details.DurationSeconds,
            RestSeconds = details.RestSeconds,
            Notes = details.Notes,
            SupersetGroupId = details.SupersetGroupId
        };
    }

    internal void UpdateOrder(int order) => Order = order;
}
