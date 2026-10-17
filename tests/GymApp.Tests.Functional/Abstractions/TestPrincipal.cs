namespace GymApp.Tests.Functional.Abstractions;

/// <summary>
/// The identity claims minted into a test JWT. A null <see cref="UserId"/>
/// means "generate a fresh user id".
/// </summary>
public sealed record TestPrincipal(string Role)
{
    public Guid? UserId { get; init; }
    public Guid? GymId { get; init; }
    public string? StaffRole { get; init; }
    public string? AdminRole { get; init; }
}
