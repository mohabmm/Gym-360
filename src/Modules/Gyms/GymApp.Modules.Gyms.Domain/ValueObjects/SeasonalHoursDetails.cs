namespace GymApp.Modules.Gyms.Domain.ValueObjects;

/// <summary>A named date range and the opening hours that apply during it.</summary>
public sealed record SeasonalHoursDetails
{
    public required string Name { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required List<DaySchedule> DaySchedules { get; init; }
}
