using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class GymSeasonalHours : Entity
{
    private readonly List<DaySchedule> _daySchedules = new();

    private GymSeasonalHours() : base() { }

    private GymSeasonalHours(
        Guid id,
        Guid gymId,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        bool isActive) : base(id)
    {
        GymId = gymId;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
    }

    public Guid GymId { get; private set; }
    public string Name { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public IReadOnlyList<DaySchedule> DaySchedules => _daySchedules.AsReadOnly();
    public bool IsActive { get; private set; }

    public static GymSeasonalHours Create(
        Guid gymId,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        List<DaySchedule> daySchedules)
    {
        var hours = new GymSeasonalHours(Guid.NewGuid(), gymId, name, startDate, endDate, isActive: true);
        hours._daySchedules.AddRange(daySchedules);
        return hours;
    }

    public void Update(string name, DateOnly startDate, DateOnly endDate, List<DaySchedule> daySchedules)
    {
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        _daySchedules.Clear();
        _daySchedules.AddRange(daySchedules);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
