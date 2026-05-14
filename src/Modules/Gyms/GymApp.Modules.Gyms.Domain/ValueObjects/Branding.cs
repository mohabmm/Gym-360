namespace GymApp.Modules.Gyms.Domain.ValueObjects;

public sealed record Branding(
    string? LogoUrl = null,
    string? PrimaryColor = null,
    string? SecondaryColor = null,
    string? CoverPhotoUrl = null);
