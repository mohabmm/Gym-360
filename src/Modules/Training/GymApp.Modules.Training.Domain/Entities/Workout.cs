using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Events;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Domain.Entities;

public sealed class Workout : Entity
{
    private readonly List<WorkoutExercise> _workoutExercises = new();
    private readonly List<MuscleGroup> _targetMuscleGroups = new();

    private Workout() { }
    private Workout(Guid id) : base(id) { }

    public Guid TrainerId { get; private set; }
    public Guid GymId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? NameAr { get; private set; }
    public string? Description { get; private set; }
    public IReadOnlyList<WorkoutExercise> WorkoutExercises => _workoutExercises.AsReadOnly();
    public int? EstimatedDurationMinutes { get; private set; }
    public IReadOnlyList<MuscleGroup> TargetMuscleGroups => _targetMuscleGroups.AsReadOnly();
    public bool IsTemplate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Workout Create(
        Guid trainerId,
        Guid gymId,
        WorkoutDetails details,
        bool isTemplate = false)
    {
        var workout = new Workout(Guid.NewGuid())
        {
            TrainerId = trainerId,
            GymId = gymId,
            Name = details.Name,
            NameAr = details.NameAr,
            Description = details.Description,
            EstimatedDurationMinutes = details.EstimatedDurationMinutes,
            IsTemplate = isTemplate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (details.TargetMuscleGroups is not null)
            workout._targetMuscleGroups.AddRange(details.TargetMuscleGroups);

        workout.RaiseDomainEvent(new WorkoutCreatedDomainEvent(workout.Id, trainerId));

        return workout;
    }

    public WorkoutExercise AddExercise(WorkoutExerciseDetails details)
    {
        var order = _workoutExercises.Count + 1;
        var workoutExercise = WorkoutExercise.Create(Id, order, details);

        _workoutExercises.Add(workoutExercise);
        UpdatedAt = DateTime.UtcNow;

        return workoutExercise;
    }

    public void RemoveExercise(Guid workoutExerciseId)
    {
        var exercise = _workoutExercises.FirstOrDefault(e => e.Id == workoutExerciseId);
        if (exercise is not null)
        {
            _workoutExercises.Remove(exercise);
            ReorderExercises();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Update(WorkoutDetails details)
    {
        Name = details.Name;
        NameAr = details.NameAr;
        Description = details.Description;
        EstimatedDurationMinutes = details.EstimatedDurationMinutes;
        _targetMuscleGroups.Clear();
        if (details.TargetMuscleGroups is not null)
            _targetMuscleGroups.AddRange(details.TargetMuscleGroups);
        UpdatedAt = DateTime.UtcNow;
    }

    private void ReorderExercises()
    {
        for (int i = 0; i < _workoutExercises.Count; i++)
            _workoutExercises[i].UpdateOrder(i + 1);
    }
}
