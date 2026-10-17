namespace GymApp.Modules.Gyms.Domain.ValueObjects;

/// <summary>The editable shape of a class: what it is, when it runs and how many it seats.</summary>
public sealed record GymClassDetails
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required ClassSchedule Schedule { get; init; }
    public required int Capacity { get; init; }
    public required bool WaitlistEnabled { get; init; }
}
