using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class GymSeasonalHours : Entity
{
    private readonly List<DaySchedule> _daySchedules = new();

    private GymSeasonalHours() : base() { }

    private GymSeasonalHours(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    public string Name { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public IReadOnlyList<DaySchedule> DaySchedules => _daySchedules.AsReadOnly();
    public bool IsActive { get; private set; }

    public static GymSeasonalHours Create(Guid gymId, SeasonalHoursDetails details)
    {
        var hours = new GymSeasonalHours(Guid.NewGuid())
        {
            GymId = gymId,
            Name = details.Name,
            StartDate = details.StartDate,
            EndDate = details.EndDate,
            IsActive = true
        };
        hours._daySchedules.AddRange(details.DaySchedules);
        return hours;
    }

    public void Update(SeasonalHoursDetails details)
    {
        Name = details.Name;
        StartDate = details.StartDate;
        EndDate = details.EndDate;
        _daySchedules.Clear();
        _daySchedules.AddRange(details.DaySchedules);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
