namespace GymApp.Modules.Scheduling.Domain.ValueObjects;

public sealed record MemberLogDetails
{
    public required int Rating { get; init; }
    public required int PerceivedExertion { get; init; }
    public string? PersonalNotes { get; init; }
    public string? Discomfort { get; init; }
    public required int Satisfaction { get; init; }
}
