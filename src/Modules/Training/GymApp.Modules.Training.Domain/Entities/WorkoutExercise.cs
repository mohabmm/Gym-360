using GymApp.Common.Domain;

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

    internal static WorkoutExercise Create(
        Guid workoutId,
        Guid exerciseId,
        int order,
        int sets,
        string reps,
        decimal? weight = null,
        int? durationSeconds = null,
        int? restSeconds = null,
        string? notes = null,
        Guid? supersetGroupId = null)
    {
        return new WorkoutExercise(Guid.NewGuid())
        {
            WorkoutId = workoutId,
            ExerciseId = exerciseId,
            Order = order,
            Sets = sets,
            Reps = reps,
            Weight = weight,
            DurationSeconds = durationSeconds,
            RestSeconds = restSeconds,
            Notes = notes,
            SupersetGroupId = supersetGroupId
        };
    }

    internal void UpdateOrder(int order) => Order = order;
}
