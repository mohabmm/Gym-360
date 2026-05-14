using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Events;

namespace GymApp.Modules.Training.Domain.Entities;

public sealed class WorkoutLog : Entity
{
    private readonly List<LogSet> _logSets = new();
    private readonly List<MuscleGroup> _muscleGroupsWorked = new();

    private WorkoutLog() { }
    private WorkoutLog(Guid id) : base(id) { }

    public Guid UserId { get; private set; }
    public Guid GymId { get; private set; }
    public Guid? WorkoutId { get; private set; }
    public DateOnly Date { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int? DurationMinutes { get; private set; }
    public string? Notes { get; private set; }
    public IReadOnlyList<LogSet> LogSets => _logSets.AsReadOnly();
    public IReadOnlyList<MuscleGroup> MuscleGroupsWorked => _muscleGroupsWorked.AsReadOnly();
    public int? PerceivedExertion { get; private set; }

    public static WorkoutLog Create(
        Guid userId,
        Guid gymId,
        DateOnly date,
        DateTime startedAt,
        Guid? workoutId = null,
        string? notes = null,
        List<MuscleGroup>? muscleGroupsWorked = null,
        int? perceivedExertion = null)
    {
        var log = new WorkoutLog(Guid.NewGuid())
        {
            UserId = userId,
            GymId = gymId,
            WorkoutId = workoutId,
            Date = date,
            StartedAt = startedAt,
            Notes = notes,
            PerceivedExertion = perceivedExertion
        };

        if (muscleGroupsWorked is not null)
            log._muscleGroupsWorked.AddRange(muscleGroupsWorked);

        log.RaiseDomainEvent(new WorkoutLoggedDomainEvent(log.Id, userId, gymId));

        return log;
    }

    public LogSet AddSet(
        Guid exerciseId,
        int setNumber,
        int reps,
        decimal? weight = null,
        WeightUnit weightUnit = WeightUnit.Kg,
        int? durationSeconds = null,
        bool isCompleted = true,
        string? notes = null)
    {
        var logSet = LogSet.Create(Id, exerciseId, setNumber, reps, weight, weightUnit, durationSeconds, isCompleted, notes);
        _logSets.Add(logSet);
        return logSet;
    }

    public void Complete(DateTime completedAt, int? durationMinutes = null)
    {
        CompletedAt = completedAt;
        DurationMinutes = durationMinutes;
    }
}
