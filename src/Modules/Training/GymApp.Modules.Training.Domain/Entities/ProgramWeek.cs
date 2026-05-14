using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Entities;

public sealed class ProgramWeek : Entity
{
    private readonly List<ProgramDayWorkout> _dayWorkouts = new();

    private ProgramWeek() { }
    private ProgramWeek(Guid id) : base(id) { }

    public Guid ProgramId { get; private set; }
    public int WeekNumber { get; private set; }
    public string? Theme { get; private set; }
    public IReadOnlyList<ProgramDayWorkout> DayWorkouts => _dayWorkouts.AsReadOnly();

    internal static ProgramWeek Create(
        Guid programId,
        int weekNumber,
        string? theme = null,
        List<ProgramDayWorkout>? dayWorkouts = null)
    {
        var week = new ProgramWeek(Guid.NewGuid())
        {
            ProgramId = programId,
            WeekNumber = weekNumber,
            Theme = theme
        };

        if (dayWorkouts is not null)
            week._dayWorkouts.AddRange(dayWorkouts);

        return week;
    }

    public void AddDayWorkout(DayOfWeek dayOfWeek, Guid workoutId)
    {
        _dayWorkouts.Add(new ProgramDayWorkout(dayOfWeek, workoutId));
    }

    public void ClearDayWorkouts() => _dayWorkouts.Clear();
}
