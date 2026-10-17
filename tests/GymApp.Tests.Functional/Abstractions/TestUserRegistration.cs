namespace GymApp.Tests.Functional.Abstractions;

/// <summary>
/// Overrides for <see cref="BaseFunctionalTest.RegisterTestUser"/>. A null
/// <see cref="Email"/> means "generate a unique address".
/// </summary>
public sealed record TestUserRegistration
{
    public string? Email { get; init; }
    public string Password { get; init; } = "StrongP@ss123!";
    public string Role { get; init; } = "Trainee";
}
