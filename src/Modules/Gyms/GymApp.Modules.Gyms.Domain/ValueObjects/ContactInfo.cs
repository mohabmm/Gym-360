namespace GymApp.Modules.Gyms.Domain.ValueObjects;

public sealed record ContactInfo(
    string Email,
    string Phone,
    string? Website = null);
