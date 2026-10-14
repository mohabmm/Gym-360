using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Events;

namespace GymApp.Modules.Training.Domain.Entities;

public sealed class Exercise : Entity
{
    private readonly List<MuscleGroup> _muscleGroups = new();

    private Exercise() { }
    private Exercise(Guid id) : base(id) { }

    public string Name { get; private set; } = default!;
    public string? NameAr { get; private set; }
    public string? Description { get; private set; }
    public string? Instructions { get; private set; }
    public string? DemoMediaUrl { get; private set; }
    public IReadOnlyList<MuscleGroup> MuscleGroups => _muscleGroups.AsReadOnly();
    public Equipment Equipment { get; private set; }
    public ExerciseCategory Category { get; private set; }
    public ExerciseScope Scope { get; private set; }
    public Guid? GymId { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public bool IsActive { get; private set; }

    public static Exercise Create(
        string name,
        Equipment equipment,
        ExerciseCategory category,
        ExerciseScope scope,
        List<MuscleGroup> muscleGroups,
        string? nameAr = null,
        string? description = null,
        string? instructions = null,
        string? demoMediaUrl = null,
        Guid? gymId = null,
        Guid? createdBy = null)
    {
        var exercise = new Exercise(Guid.NewGuid())
        {
            Name = name,
            NameAr = nameAr,
            Equipment = equipment,
            Category = category,
            Scope = scope,
            Description = description,
            Instructions = instructions,
            DemoMediaUrl = demoMediaUrl,
            GymId = gymId,
            CreatedBy = createdBy,
            IsActive = true
        };

        exercise._muscleGroups.AddRange(muscleGroups);
        exercise.RaiseDomainEvent(new ExerciseCreatedDomainEvent(exercise.Id));

        return exercise;
    }

    public void SetArabicName(string? nameAr) =>
        NameAr = string.IsNullOrWhiteSpace(nameAr) ? null : nameAr.Trim();

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void Update(
        string name,
        string? description,
        string? instructions,
        string? demoMediaUrl,
        Equipment equipment,
        ExerciseCategory category,
        List<MuscleGroup> muscleGroups)
    {
        Name = name;
        Description = description;
        Instructions = instructions;
        DemoMediaUrl = demoMediaUrl;
        Equipment = equipment;
        Category = category;
        _muscleGroups.Clear();
        _muscleGroups.AddRange(muscleGroups);
    }
}
