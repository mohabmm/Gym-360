using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Events;
using GymApp.Modules.Training.Domain.ValueObjects;

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
        ExerciseDetails details,
        ExerciseScope scope,
        Guid? gymId = null,
        Guid? createdBy = null)
    {
        var exercise = new Exercise(Guid.NewGuid())
        {
            Name = details.Name,
            NameAr = details.NameAr,
            Equipment = details.Equipment,
            Category = details.Category,
            Scope = scope,
            Description = details.Description,
            Instructions = details.Instructions,
            DemoMediaUrl = details.DemoMediaUrl,
            GymId = gymId,
            CreatedBy = createdBy,
            IsActive = true
        };

        exercise._muscleGroups.AddRange(details.MuscleGroups);
        exercise.RaiseDomainEvent(new ExerciseCreatedDomainEvent(exercise.Id));

        return exercise;
    }

    public void SetArabicName(string? nameAr) =>
        NameAr = string.IsNullOrWhiteSpace(nameAr) ? null : nameAr.Trim();

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
