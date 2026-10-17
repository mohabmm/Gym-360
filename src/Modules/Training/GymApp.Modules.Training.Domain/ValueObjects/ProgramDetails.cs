namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>What a training program is: its names, description and length in weeks.</summary>
public sealed record ProgramDetails
{
    public required string Name { get; init; }
    public required int DurationWeeks { get; init; }
    public string? NameAr { get; init; }
    public string? Description { get; init; }
}
