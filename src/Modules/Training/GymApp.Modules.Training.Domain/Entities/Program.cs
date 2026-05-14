using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Events;

namespace GymApp.Modules.Training.Domain.Entities;

public sealed class Program : Entity
{
    private readonly List<ProgramWeek> _programWeeks = new();

    private Program() { }
    private Program(Guid id) : base(id) { }

    public Guid TrainerId { get; private set; }
    public Guid GymId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? NameAr { get; private set; }
    public string? Description { get; private set; }
    public int DurationWeeks { get; private set; }
    public IReadOnlyList<ProgramWeek> ProgramWeeks => _programWeeks.AsReadOnly();
    public bool IsTemplate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Program Create(
        Guid trainerId,
        Guid gymId,
        string name,
        int durationWeeks,
        string? nameAr = null,
        string? description = null,
        bool isTemplate = false)
    {
        if (durationWeeks < 1 || durationWeeks > 52)
            throw new ArgumentOutOfRangeException(nameof(durationWeeks), "Duration must be between 1 and 52 weeks.");

        var program = new Program(Guid.NewGuid())
        {
            TrainerId = trainerId,
            GymId = gymId,
            Name = name,
            NameAr = nameAr,
            DurationWeeks = durationWeeks,
            Description = description,
            IsTemplate = isTemplate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        program.RaiseDomainEvent(new ProgramCreatedDomainEvent(program.Id, trainerId));

        return program;
    }

    public ProgramWeek AddWeek(int weekNumber, string? theme = null, List<ProgramDayWorkout>? dayWorkouts = null)
    {
        var week = ProgramWeek.Create(Id, weekNumber, theme, dayWorkouts);
        _programWeeks.Add(week);
        UpdatedAt = DateTime.UtcNow;
        return week;
    }

    public void Update(string name, string? description, int durationWeeks)
    {
        if (durationWeeks < 1 || durationWeeks > 52)
            throw new ArgumentOutOfRangeException(nameof(durationWeeks), "Duration must be between 1 and 52 weeks.");

        Name = name;
        Description = description;
        DurationWeeks = durationWeeks;
        UpdatedAt = DateTime.UtcNow;
    }
}
