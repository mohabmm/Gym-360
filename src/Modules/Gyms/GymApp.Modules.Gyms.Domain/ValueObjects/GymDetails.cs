namespace GymApp.Modules.Gyms.Domain.ValueObjects;

/// <summary>What a gym is: its names, where it is, how to reach it and its join code.</summary>
public sealed record GymDetails
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required Address Address { get; init; }
    public required ContactInfo ContactInfo { get; init; }
    public required GymCode GymCode { get; init; }
}
